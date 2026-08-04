using System.Security.Cryptography;
using System.Text;
using Hambaft.Domain;

namespace Hambaft.Application;

public interface IEntityEndingResolver
{
    EndingResult Resolve(StoryPackage package,StorySession state,WorldEntity entity,IReadOnlyList<IDomainEvent> history,long resolvedAtStreamVersion,DateTimeOffset resolvedAtUtc);
}
public interface IWorldEndingResolver
{
    EndingResult Resolve(StoryPackage package,StorySession state,IReadOnlyList<IDomainEvent> history,long resolvedAtStreamVersion,DateTimeOffset resolvedAtUtc);
}

public sealed class DeterministicEndingResolver(IConditionEngine conditions) : IEntityEndingResolver,IWorldEndingResolver
{
    public EndingResult Resolve(StoryPackage package,StorySession state,WorldEntity entity,IReadOnlyList<IDomainEvent> history,long version,DateTimeOffset utc)
    {
        var team=state.Teams.SingleOrDefault(x=>x.ControlledEntityId==entity.Id);
        var candidates=package.EntityEndingDefinitions
            .Where(x=>x.EligibleEntityDefinitions.Contains(entity.DefinitionId,StringComparer.Ordinal)&&conditions.EvaluateAll(x.Conditions,new(state,package,team?.Id,entity.Id)).Result)
            .ToList();
        var selected=Select(candidates,x=>x.Priority,x=>x.Weight,x=>x.Id,package,state,entity.Id);
        var evidence=Evidence(selected.EvidenceSelectors,state,history,EndingScope.Entity,entity.Id);
        return Result(package,state,EndingScope.Entity,entity.Id,selected.Id,selected.BodyNarrativeRef,selected.PublicSummaryNarrativeRef,selected.PresentationTags,evidence,history,version,utc);
    }

    public EndingResult Resolve(StoryPackage package,StorySession state,IReadOnlyList<IDomainEvent> history,long version,DateTimeOffset utc)
    {
        var candidates=package.WorldEndingDefinitions.Where(x=>conditions.EvaluateAll(x.Conditions,new(state,package)).Result).ToList();
        var selected=Select(candidates,x=>x.Priority,x=>x.Weight,x=>x.Id,package,state,state.Id);
        var evidence=Evidence(selected.EvidenceSelectors,state,history,EndingScope.World,state.Id);
        return Result(package,state,EndingScope.World,state.Id,selected.Id,selected.BodyNarrativeRef,null,selected.PresentationTags,evidence,history,version,utc);
    }

    private static T Select<T>(IReadOnlyList<T> eligible,Func<T,int> priority,Func<T,int> weight,Func<T,string> id,StoryPackage package,StorySession state,Guid scopeId)
    {
        if(eligible.Count==0)throw new DomainException("No eligible Ending definition exists.");
        var top=eligible.Where(x=>priority(x)==eligible.Max(priority)).OrderBy(id,StringComparer.Ordinal).ToList();
        if(top.Count==1)return top[0];
        var total=top.Sum(weight);var bytes=SHA256.HashData(Encoding.UTF8.GetBytes($"{package.ContentHash}|{state.Seed}|{state.DifficultyId}|{scopeId:D}|{string.Join('|',top.Select(id))}"));
        var pick=(int)(BitConverter.ToUInt64(bytes,0)%(ulong)total);
        foreach(var item in top){pick-=weight(item);if(pick<0)return item;}
        return top[^1];
    }

    private static EndingResult Result(StoryPackage package,StorySession state,EndingScope scope,Guid scopeId,string id,string narrative,string? summary,IReadOnlyDictionary<string,string> tags,IReadOnlyList<EndingEvidence> evidence,IReadOnlyList<IDomainEvent> history,long version,DateTimeOffset utc)
    {
        var fingerprint=Fingerprint(package,state,history);
        var resultId=DeterministicIds.Create(state.Id,scope,scopeId,id,version,fingerprint);
        var snapshot=state.Metrics.Where(m=>scope==EndingScope.World||m.Scope==MetricScope.World||m.Scope==MetricScope.Entity&&m.ScopeId==scopeId)
            .OrderBy(m=>m.Scope).ThenBy(m=>m.ScopeId).ThenBy(m=>m.MetricKey,StringComparer.Ordinal).Select(m=>new EndingMetricSnapshot(m.Scope,m.ScopeId,m.MetricKey,m.NumericValue)).ToList();
        return new(resultId,state.Id,scope,scopeId,id,package.Manifest.Id,package.Manifest.Version,package.ContentHash,version,fingerprint,narrative,new Dictionary<string,string>(tags,StringComparer.Ordinal),utc,evidence,snapshot,summary);
    }

    private static string Fingerprint(StoryPackage package,StorySession state,IReadOnlyList<IDomainEvent> history)
    {
        var text=new StringBuilder().Append(package.ContentHash).Append('|').Append(state.Seed).Append('|').Append(state.DifficultyId);
        foreach(var e in history)text.Append('|').Append(e.GetType().Name).Append(':').Append(e.Metadata.CommandId.ToString("N"));
        foreach(var metric in state.Metrics.OrderBy(x=>x.Scope).ThenBy(x=>x.ScopeId).ThenBy(x=>x.MetricKey,StringComparer.Ordinal))text.Append('|').Append(metric.Scope).Append(':').Append(metric.ScopeId.ToString("N")).Append(':').Append(metric.MetricKey).Append('=').Append(metric.NumericValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return "sha256:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()))).ToLowerInvariant();
    }

    private static IReadOnlyList<EndingEvidence> Evidence(IReadOnlyList<EndingEvidenceSelector> selectors,StorySession state,IReadOnlyList<IDomainEvent> history,EndingScope scope,Guid scopeId)
    {
        var evidence=new List<EndingEvidence>();var isWorld=scope==EndingScope.World;
        foreach(var selector in selectors)
        {
            switch(selector.Type)
            {
                case EndingEvidenceSelectorType.Choice:
                    evidence.AddRange(state.SubmittedChoices.Where(x=>selector.Id is null||x.ChoiceId==selector.Id).OrderBy(x=>x.SubmittedAtStreamVersion).Select(x=>new EndingEvidence(EndingEvidenceKind.Choice,x.CommandId,x.ChoiceId,null,x.AssignmentId.ToString("D"),isWorld)));
                    break;
                case EndingEvidenceSelectorType.Proposal:
                    evidence.AddRange(state.Proposals.Where(x=>selector.Id is null||x.InteractionTypeId==selector.Id).OrderBy(x=>x.ProposalId).Select(x=>new EndingEvidence(EndingEvidenceKind.Proposal,x.ProposalId,x.InteractionTypeId,null,x.Status.ToString(),isWorld)));
                    break;
                case EndingEvidenceSelectorType.Agreement:
                    evidence.AddRange(state.Agreements.Where(x=>selector.Id is null||x.InteractionTypeId==selector.Id).OrderBy(x=>x.AgreementId).Select(x=>new EndingEvidence(EndingEvidenceKind.Agreement,x.AgreementId,x.InteractionTypeId,null,x.Status.ToString(),isWorld&&x.Visibility==AgreementVisibility.Public)));
                    break;
                case EndingEvidenceSelectorType.Memory:
                    evidence.AddRange(state.Memories.Where(x=>(selector.Key is null||x.Key==selector.Key)&&(isWorld||x.ScopeId==scopeId)).OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new EndingEvidence(EndingEvidenceKind.Memory,null,null,x.Key,x.OptionalJsonValue,isWorld&&x.Visibility==MemoryVisibility.Public)));
                    break;
                case EndingEvidenceSelectorType.Metric:
                    evidence.AddRange(state.Metrics.Where(x=>(selector.Key is null||x.MetricKey==selector.Key)&&(isWorld||x.Scope==MetricScope.World||x.ScopeId==scopeId)).OrderBy(x=>x.MetricKey,StringComparer.Ordinal).Select(x=>new EndingEvidence(EndingEvidenceKind.MetricSnapshot,null,null,x.MetricKey,x.NumericValue.ToString(System.Globalization.CultureInfo.InvariantCulture),isWorld||x.Scope==MetricScope.World)));
                    break;
                case EndingEvidenceSelectorType.Relationship:
                    evidence.AddRange(state.Relationships.Where(x=>(selector.Key is null||x.RelationshipKey==selector.Key)&&(isWorld||x.SourceEntityId==scopeId||x.TargetEntityId==scopeId)).OrderBy(x=>x.SourceEntityId).ThenBy(x=>x.TargetEntityId).Select(x=>new EndingEvidence(EndingEvidenceKind.Relationship,null,null,x.RelationshipKey,$"{x.SourceEntityId:D}>{x.TargetEntityId:D}:{x.NumericValue}",isWorld)));
                    break;
                case EndingEvidenceSelectorType.BehaviorAction:
                    evidence.AddRange(state.AuthoredBehaviorSelections.Where(x=>selector.Id is null||x.ActionId==selector.Id).OrderBy(x=>x.EntityId).Select(x=>new EndingEvidence(EndingEvidenceKind.BehaviorAction,x.EntityId,x.ActionId,null,x.CheckpointId,isWorld)));
                    break;
                case EndingEvidenceSelectorType.Consequence:
                    evidence.AddRange(state.ScheduledConsequences.Where(x=>selector.Id is null||x.DefinitionId==selector.Id).OrderBy(x=>x.ScheduledConsequenceId).Select(x=>new EndingEvidence(EndingEvidenceKind.Consequence,x.ScheduledConsequenceId,x.DefinitionId,null,x.Status.ToString(),isWorld&&x.Visibility==ConsequenceVisibility.Public)));
                    break;
                case EndingEvidenceSelectorType.DomainEvent:
                    evidence.AddRange(history.Where(x=>selector.Id is null||x.GetType().Name==selector.Id).Select(x=>new EndingEvidence(EndingEvidenceKind.DomainEvent,x.Metadata.CommandId,x.GetType().Name,null,x.Metadata.CheckpointId,isWorld)));
                    break;
            }
        }
        return evidence.Distinct().OrderBy(x=>x.Kind).ThenBy(x=>x.StableId,StringComparer.Ordinal).ThenBy(x=>x.ReferenceId).ToList();
    }
}
