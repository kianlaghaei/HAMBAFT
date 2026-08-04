using System.Security.Cryptography;
using System.Text;
using Hambaft.Domain;

namespace Hambaft.Application;

public sealed record ResolvedBehaviorAction(Guid EntityId,string BehaviorProfileId,string? RuleId,BehaviorActionDefinition Action);

public interface IBehaviorResolver
{
    IReadOnlyList<ResolvedBehaviorAction> Resolve(StorySession state,StoryPackage package);
}

public sealed class DeterministicBehaviorResolver(IConditionEngine conditions) : IBehaviorResolver
{
    public IReadOnlyList<ResolvedBehaviorAction> Resolve(StorySession state,StoryPackage package)
    {
        var result=new List<ResolvedBehaviorAction>();
        foreach(var entity in state.Entities.Where(x=>x.Status==EntityStatus.Active&&x.ControllerType==ControllerType.AuthoredBehavior).OrderBy(x=>x.DefinitionId,StringComparer.Ordinal).ThenBy(x=>x.Id))
        {
            var profile=package.BehaviorDefinitions.Profiles.SingleOrDefault(x=>x.Id==entity.BehaviorProfileId)??throw new DomainException($"Behavior Profile '{entity.BehaviorProfileId}' is missing.");
            if(!profile.EligibleEntityDefinitions.Contains(entity.DefinitionId,StringComparer.Ordinal))throw new DomainException($"Behavior Profile '{profile.Id}' is not eligible for Entity '{entity.DefinitionId}'.");
            var eligible=profile.Rules.Where(rule=>RepeatAllowed(rule,state,entity)).Where(rule=>conditions.EvaluateAll(rule.Conditions,new(state,package,entity.ControlledByTeamId,entity.Id),false).Result).ToList();
            string? ruleId=null;string actionId;
            if(eligible.Count==0) actionId=PreferredFallback(profile,package,state.DifficultyId);
            else
            {
                var priority=eligible.Max(x=>x.Priority);var candidates=eligible.Where(x=>x.Priority==priority).OrderBy(x=>x.Id,StringComparer.Ordinal).ToList();
                var selected=SelectWeighted(candidates,state,package,entity);ruleId=selected.Id;actionId=selected.ActionId;
            }
            var action=package.BehaviorDefinitions.Actions.SingleOrDefault(x=>x.Id==actionId)??throw new DomainException($"Behavior Action '{actionId}' is missing.");
            result.Add(new(entity.Id,profile.Id,ruleId,action));
        }
        return result;
    }

    private static BehaviorRuleDefinition SelectWeighted(IReadOnlyList<BehaviorRuleDefinition> candidates,StorySession state,StoryPackage package,WorldEntity entity)
    {
        if(candidates.Count==1)return candidates[0];
        var difficulty=package.DifficultyDefinitions.SingleOrDefault(x=>x.Id==state.DifficultyId);
        var weighted=candidates.Select(rule=>new
        {
            Rule=rule,
            Weight=Math.Max(0m,rule.Weight*rule.DifficultyModifiers.Where(x=>x.DifficultyId==state.DifficultyId).Select(x=>x.Multiplier).DefaultIfEmpty(1m).Aggregate((a,b)=>a*b)*
                (difficulty?.BehaviorRuleWeightModifiers.Where(x=>x.RuleId==rule.Id).Select(x=>x.Multiplier).DefaultIfEmpty(1m).Aggregate((a,b)=>a*b)??1m))
        }).ToList();
        var total=weighted.Sum(x=>x.Weight);if(total<=0)return candidates[0];
        var material=string.Join("|",package.ContentHash,state.Seed,state.DifficultyId,state.StateVersion,state.CurrentCheckpointId,entity.DefinitionId,entity.Id,string.Join(",",weighted.Select(x=>$"{x.Rule.Id}:{x.Weight}")));
        var bytes=SHA256.HashData(Encoding.UTF8.GetBytes(material));var unit=BitConverter.ToUInt64(bytes,0)/(decimal)ulong.MaxValue;var point=unit*total;
        foreach(var candidate in weighted){if(point<candidate.Weight)return candidate.Rule;point-=candidate.Weight;}return weighted[^1].Rule;
    }

    private static bool RepeatAllowed(BehaviorRuleDefinition rule,StorySession state,WorldEntity entity)=>rule.RepeatPolicy switch
    {
        RepeatPolicy.OncePerSession=>!state.AuthoredBehaviorSelections.Any(x=>x.EntityId==entity.Id&&x.RuleId==rule.Id),
        RepeatPolicy.OncePerCheckpoint=>!state.AuthoredBehaviorSelections.Any(x=>x.EntityId==entity.Id&&x.RuleId==rule.Id&&x.CheckpointId==state.CurrentCheckpointId),
        RepeatPolicy.Repeatable=>true,
        _=>false
    };
    private static string PreferredFallback(BehaviorProfileDefinition profile,StoryPackage package,string difficultyId)
    {
        var preferred=package.DifficultyDefinitions.SingleOrDefault(x=>x.Id==difficultyId)?.FallbackActionPreference;
        return preferred is not null&&package.BehaviorDefinitions.Actions.Any(x=>x.Id==preferred)?preferred:profile.FallbackActionId;
    }
}

public sealed partial class SessionRuntime
{
    public async Task<CommandResult> ExecuteAsync(ResolveUncontrolledEntityActions c,CancellationToken ct)
    {
        RequireStoryServices();var state=await Load(c.SessionId,c.ExpectedVersion,ct);var package=await LoadLockedPackage(state,ct);var appended=new List<IDomainEvent>();ResolveBehaviorEvents(state,package,c.Context,appended);
        if(appended.Count>0)await store.AppendAsync(c.SessionId,c.ExpectedVersion,appended,state,ct);
        return new(state.Id,state.StateVersion,appended.Count==0?"NoBehaviorActions":nameof(AuthoredBehaviorActionSelected),EmittedEventTypes:appended.Select(x=>x.GetType().Name).ToList());
    }

    private void ResolveBehaviorEvents(StorySession state,StoryPackage package,CommandContext context,List<IDomainEvent> appended)
    {
        if(behaviorResolver is null)
        {
            if(state.Entities.Any(x=>x.ControllerType==ControllerType.AuthoredBehavior))throw new InvalidOperationException("Behavior Resolver is not configured.");return;
        }
        foreach(var resolved in behaviorResolver.Resolve(state,package))
        {
            var entity=state.Entities.Single(x=>x.Id==resolved.EntityId);var metadata=context.For(state.Id) with { TeamId=entity.ControlledByTeamId,CheckpointId=state.CurrentCheckpointId,CausationId=context.CommandId };
            Add(state,appended,new AuthoredBehaviorActionSelected(entity.Id,resolved.BehaviorProfileId,resolved.RuleId,resolved.Action.Id,state.DifficultyId,state.CheckpointResolutionNumber+1,metadata));
            foreach(var effectId in resolved.Action.EffectIds)
            {
                var effect=package.Effects.Single(x=>x.Id==effectId);
                foreach(var @event in effects!.CreateEvents(effect,new(state,package,null,null,null,metadata,state.StateVersion+1,entity.ControlledByTeamId,entity.Id)))Add(state,appended,@event);
            }
            if(resolved.Action.NarrativeRef is { } narrative)
            {
                if(!package.Narrative.TryGetValue(package.Manifest.DefaultLocale,out var locale)||!locale.ContainsKey(narrative))throw new DomainException($"Behavior narrative '{narrative}' is missing.");
                Add(state,appended,new WorldNarrativePublished($"behavior:{resolved.Action.Id}",narrative,state.CurrentCheckpointId!,state.CurrentNarrativeRevision+1,"$behavior",metadata));
            }
        }
    }
}
