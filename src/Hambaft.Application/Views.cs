using Hambaft.Domain;
using System.Text.Json;

namespace Hambaft.Application;

public sealed record NarrativeChoiceView(string Id,string Label,string? ShortOutcome);
public sealed record NarrativeContentView(string StoryletId,string NarrativeRef,string Title,IReadOnlyList<string> Paragraphs,IReadOnlyDictionary<string,string> PresentationTags);
public sealed record TeamStoryletView(Guid AssignmentId,string StoryletId,string CheckpointId,string Title,IReadOnlyList<string> Paragraphs,IReadOnlyDictionary<string,string> PresentationTags,IReadOnlyList<NarrativeChoiceView> Choices,bool RequiredResponse,bool Submitted,string? SubmittedChoiceId);
public sealed record EndingPresentationView(Guid EndingResultId,EndingScope Scope,Guid ScopeId,string EndingDefinitionId,string Title,IReadOnlyList<string> Paragraphs,IReadOnlyDictionary<string,string> PresentationTags,IReadOnlyList<EndingEvidence> Evidence,string ContentHash,long ResolvedAtStreamVersion);
public sealed record SessionMetadataView(Guid SessionId,SessionStatus Status,string StoryPackageId,string StoryVersion,string ContentHash,string DifficultyId,string? CurrentCheckpointId);
public sealed record EndingEligibilityDiagnostic(EndingScope Scope,Guid ScopeId,string EndingDefinitionId,bool Eligible,int Priority);
public sealed record AdminSessionView(Guid Id,IReadOnlyList<Guid> TeamsAwaitingResponse,IReadOnlyList<string> AssignedStoryletIds,string? Checkpoint,IReadOnlyList<Proposal> OpenProposals,IReadOnlyList<Agreement> ActiveAgreements,IReadOnlyList<ScheduledConsequence> PendingConsequences,IReadOnlyList<AuthoredBehaviorSelection> UncontrolledBehaviorSelections,IReadOnlyList<EndingEligibilityDiagnostic> EndingEligibilityDiagnostics,string PackageVersion,string ContentHash,long StreamVersion);

public sealed record SessionStateView(
    Guid Id,string StoryPackageId,string StoryVersion,string ContentHash,int Seed,SessionStatus Status,
    DateTimeOffset CreatedAtUtc,DateTimeOffset? StartedAtUtc,DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<Team> Teams,IReadOnlyList<WorldEntity> Entities,long StateVersion,
    string? CurrentCheckpointId,bool NarrativeInitialized,IReadOnlyList<StoryletAssignment> StoryletAssignments,IReadOnlyList<SubmittedStoryChoice> SubmittedChoices,
    string DifficultyId="standard",int CheckpointResolutionNumber=0,IReadOnlyList<Proposal>? Proposals=null,IReadOnlyList<ProposalRevision>? ProposalRevisions=null,IReadOnlyList<Agreement>? Agreements=null,IReadOnlyList<ScheduledConsequence>? ScheduledConsequences=null,IReadOnlyList<AuthoredBehaviorSelection>? AuthoredBehaviorSelections=null,IReadOnlyList<EndingResult>? EndingResults=null)
{
    public IReadOnlyList<Guid> TeamsAwaitingResponse=>StoryletAssignments.Where(x=>x.RequiredResponse&&x.Status==StoryletAssignmentStatus.Assigned&&x.TargetTeamId is not null).Select(x=>x.TargetTeamId!.Value).Distinct().OrderBy(x=>x).ToList();
    public IReadOnlyList<string> AssignedStoryletIds=>StoryletAssignments.Select(x=>x.StoryletId).ToList();
    public IReadOnlyList<string> SubmittedChoiceIds=>SubmittedChoices.Select(x=>x.ChoiceId).ToList();
}

public sealed record ProposalRevisionView(int RevisionNumber,Guid CreatedByTeamId,JsonElement TermsPayload,DateTimeOffset CreatedAtUtc);
public sealed record ProposalItemView(Guid ProposalId,string InteractionTypeId,Guid SenderTeamId,Guid ReceiverTeamId,int CurrentRevisionNumber,JsonElement TermsPayload,ProposalStatus Status,string DeadlineCheckpoint,IReadOnlyList<string> AllowedActions,long StateVersion,IReadOnlyList<ProposalRevisionView>? Revisions=null);
public sealed record ProposalInboxView(Guid Id,Guid SessionId,IReadOnlyList<ProposalItemView> Incoming,IReadOnlyList<ProposalItemView> Outgoing,long StateVersion);
public sealed record AgreementView(Guid AgreementId,string InteractionTypeId,IReadOnlyList<Guid> Parties,JsonElement TermsPayload,AgreementStatus Status,string ActivationCheckpoint,string? ExecutionCheckpoint,AgreementVisibility Visibility);
public sealed record ScheduledConsequenceView(Guid ScheduledConsequenceId,string DefinitionId,string? DueCheckpointId,ConsequenceTriggerType TriggerType,ScheduledConsequenceStatus Status,ConsequenceVisibility Visibility);
public sealed record AvailableInteractionView(string InteractionTypeId,IReadOnlyList<Guid> AllowedTargetTeamIds);

public sealed record TeamExperienceView(
    Guid Id,Guid SessionId,Team Team,WorldEntity? ControlledEntity,IReadOnlyList<Metric> VisibleMetrics,
    IReadOnlyList<StoryMemory> VisibleMemories,IReadOnlyList<Relationship> VisibleRelationships,long StateVersion,
    string? CurrentCheckpointId,IReadOnlyList<TeamStoryletView> PrivateStorylets,
    IReadOnlyList<ProposalItemView>? Inbox=null,IReadOnlyList<ProposalItemView>? Outbox=null,IReadOnlyList<AgreementView>? Agreements=null,IReadOnlyList<AvailableInteractionView>? AvailableInteractionTypes=null,IReadOnlyList<ScheduledConsequenceView>? PendingConsequences=null,EndingPresentationView? EntityEnding=null,EndingPresentationView? WorldEnding=null,SessionMetadataView? SessionMetadata=null)
{
    public IReadOnlyList<AgreementView> ActiveAgreements => (Agreements??[]).Where(x=>x.Status==AgreementStatus.Active).ToList();
}

public sealed record PublicWorldView(
    Guid Id,SessionStatus Status,IReadOnlyList<WorldEntity> Entities,IReadOnlyList<Metric> WorldMetrics,
    IReadOnlyList<StoryMemory> PublicMemories,IReadOnlyList<Relationship> PublicRelationships,long StateVersion,
    string StoryPackageId,string StoryVersion,string ContentHash,string? CurrentCheckpointId,string? CurrentWorldStoryletId,string? CurrentWorldNarrativeRef,NarrativeContentView? Narrative,
    IReadOnlyList<AgreementView>? PublicAgreements=null,IReadOnlyList<ScheduledConsequenceView>? PublicConsequences=null,EndingPresentationView? WorldEnding=null,IReadOnlyList<EndingPresentationView>? PublicEntityEndingSummaries=null,string DifficultyId="standard");

public sealed record EntityStateView(Guid Id,Guid SessionId,ControllerType ControllerType,Guid? ControlledByTeamId,EntityStatus Status,IReadOnlyList<Metric> Metrics,IReadOnlyList<StoryMemory> VisibleMemories,long StateVersion);

public sealed record SessionExperienceView(
    Guid Id,SessionStatus Status,IReadOnlyList<Team> Teams,IReadOnlyList<WorldEntity> Entities,IReadOnlyList<Metric> Metrics,
    IReadOnlyList<StoryMemory> Memories,IReadOnlyList<Relationship> Relationships,long StateVersion,
    string StoryPackageId,string StoryVersion,string ContentHash,string? CurrentCheckpointId,
    IReadOnlyList<StoryletAssignment> StoryletAssignments,IReadOnlyList<SubmittedStoryChoice> SubmittedChoices,
    string DifficultyId="standard",int CheckpointResolutionNumber=0,IReadOnlyList<Proposal>? Proposals=null,IReadOnlyList<ProposalRevision>? ProposalRevisions=null,IReadOnlyList<Agreement>? Agreements=null,IReadOnlyList<ScheduledConsequence>? ScheduledConsequences=null,IReadOnlyList<AuthoredBehaviorSelection>? AuthoredBehaviorSelections=null,IReadOnlyList<EndingResult>? EndingResults=null);

public sealed record EndingEvidenceView(Guid Id,IReadOnlyList<EndingResult> EntityEndings,EndingResult? WorldEnding,string PackageId,string PackageVersion,string ContentHash,long StateVersion);

public static class ViewProjector
{
    public static SessionStateView Session(StorySession s)=>new(s.Id,s.StoryPackageId,s.StoryVersion,s.ContentHash,s.Seed,s.Status,s.CreatedAtUtc,s.StartedAtUtc,s.CompletedAtUtc,s.Teams.ToList(),s.Entities.ToList(),s.StateVersion,s.CurrentCheckpointId,s.NarrativeInitialized,s.StoryletAssignments.ToList(),s.SubmittedChoices.ToList(),s.DifficultyId,s.CheckpointResolutionNumber,s.Proposals.ToList(),s.ProposalRevisions.ToList(),s.Agreements.ToList(),s.ScheduledConsequences.ToList(),s.AuthoredBehaviorSelections.ToList(),s.EndingResults.ToList());
    public static PublicWorldView Public(StorySession s)=>new(s.Id,s.Status,s.Entities.ToList(),s.Metrics.Where(x=>x.Scope==MetricScope.World).ToList(),s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public).ToList(),s.Relationships.ToList(),s.StateVersion,s.StoryPackageId,s.StoryVersion,s.ContentHash,s.CurrentCheckpointId,s.CurrentWorldStoryletId,s.CurrentWorldNarrativeRef,null,s.Agreements.Where(x=>x.Visibility==AgreementVisibility.Public).Select(ToAgreementView).ToList(),s.ScheduledConsequences.Where(x=>x.Visibility==ConsequenceVisibility.Public).Select(ToConsequenceView).ToList(),DifficultyId:s.DifficultyId);
    public static TeamExperienceView Team(StorySession s,Team team,StoryPackage? package=null,string? locale=null)
    {
        var entity=s.Entities.SingleOrDefault(x=>x.Id==team.ControlledEntityId);
        var metrics=s.Metrics.Where(x=>x.Scope==MetricScope.World||x.Scope==MetricScope.Team&&x.ScopeId==team.Id||entity is not null&&x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList();
        var memories=s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.TeamPrivate&&x.Scope==MemoryScope.Team&&x.ScopeId==team.Id||entity is not null&&x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList();
        var relationships=entity is null?[]:s.Relationships.Where(x=>x.SourceEntityId==entity.Id||x.TargetEntityId==entity.Id).ToList();
        var phase3=Phase3(team,s.Proposals,s.ProposalRevisions,s.Agreements,s.ScheduledConsequences,s.StateVersion,package,s.Teams,s.Entities);
        return new(team.Id,s.Id,team,entity,metrics,memories,relationships,s.StateVersion,s.CurrentCheckpointId,package is null?[]:PrivateStorylets(s.StoryletAssignments,s.SubmittedChoices,team,entity,package,locale),phase3.Incoming,phase3.Outgoing,phase3.Agreements,phase3.Interactions,phase3.Consequences,SessionMetadata:new(s.Id,s.Status,s.StoryPackageId,s.StoryVersion,s.ContentHash,s.DifficultyId,s.CurrentCheckpointId));
    }
    public static EntityStateView Entity(StorySession s,WorldEntity entity)=>new(entity.Id,s.Id,entity.ControllerType,entity.ControlledByTeamId,entity.Status,s.Metrics.Where(x=>x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList(),s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList(),s.StateVersion);
    public static TeamExperienceView Team(SessionExperienceView s,Team team,StoryPackage? package=null,string? locale=null)
    {
        var entity=s.Entities.SingleOrDefault(x=>x.Id==team.ControlledEntityId);
        var metrics=s.Metrics.Where(x=>x.Scope==MetricScope.World||x.Scope==MetricScope.Team&&x.ScopeId==team.Id||entity is not null&&x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList();
        var memories=s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.TeamPrivate&&x.Scope==MemoryScope.Team&&x.ScopeId==team.Id||entity is not null&&x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList();
        var relationships=entity is null?[]:s.Relationships.Where(x=>x.SourceEntityId==entity.Id||x.TargetEntityId==entity.Id).ToList();
        var phase3=Phase3(team,s.Proposals??[],s.ProposalRevisions??[],s.Agreements??[],s.ScheduledConsequences??[],s.StateVersion,package,s.Teams,s.Entities);
        return new(team.Id,s.Id,team,entity,metrics,memories,relationships,s.StateVersion,s.CurrentCheckpointId,package is null?[]:PrivateStorylets(s.StoryletAssignments,s.SubmittedChoices,team,entity,package,locale),phase3.Incoming,phase3.Outgoing,phase3.Agreements,phase3.Interactions,phase3.Consequences,SessionMetadata:new(s.Id,s.Status,s.StoryPackageId,s.StoryVersion,s.ContentHash,s.DifficultyId,s.CurrentCheckpointId));
    }
    public static EntityStateView Entity(SessionExperienceView s,WorldEntity entity)=>new(entity.Id,s.Id,entity.ControllerType,entity.ControlledByTeamId,entity.Status,s.Metrics.Where(x=>x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList(),s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList(),s.StateVersion);
    public static PublicWorldView Hydrate(PublicWorldView view,StoryPackage package,string? locale=null)=>view with { Narrative=view.CurrentWorldStoryletId is null||view.CurrentWorldNarrativeRef is null?null:Content(package,view.CurrentWorldStoryletId,view.CurrentWorldNarrativeRef,locale) };

    private static IReadOnlyList<TeamStoryletView> PrivateStorylets(IReadOnlyList<StoryletAssignment> assignments,IReadOnlyList<SubmittedStoryChoice> submissions,Team team,WorldEntity? entity,StoryPackage package,string? locale)
        =>assignments.Where(a=>(a.Status is StoryletAssignmentStatus.Assigned or StoryletAssignmentStatus.Responded)&&(a.TargetTeamId==team.Id||entity is not null&&a.Scope==StoryletScope.EntityPrivate&&a.TargetEntityId==entity.Id))
            .OrderBy(a=>a.AssignmentId).Select(a=>
            {
                var storylet=package.Storylets.Single(s=>s.Id==a.StoryletId); var narrative=Localized(package,storylet.NarrativeRef,locale); var submitted=submissions.SingleOrDefault(s=>s.AssignmentId==a.AssignmentId);
                return new TeamStoryletView(a.AssignmentId,a.StoryletId,a.CheckpointId,narrative.Title,narrative.Paragraphs,narrative.PresentationTags,storylet.Choices.Select(c=>{var label=Localized(package,c.LabelRef,locale);return new NarrativeChoiceView(c.Id,label.ChoiceLabel??label.Title,label.ShortOutcome);}).ToList(),a.RequiredResponse,submitted is not null,submitted?.ChoiceId);
            }).ToList();
    private static NarrativeContentView Content(StoryPackage package,string storyletId,string narrativeRef,string? locale)
    { var n=Localized(package,narrativeRef,locale); return new(storyletId,narrativeRef,n.Title,n.Paragraphs,n.PresentationTags); }
    private static NarrativeDefinition Localized(StoryPackage package,string key,string? locale)
    {
        var selected=locale is not null&&package.Narrative.ContainsKey(locale)?locale:package.Manifest.DefaultLocale;
        if(package.Narrative.TryGetValue(selected,out var dictionary)&&dictionary.TryGetValue(key,out var value))return value;
        if(package.InkDefinition.References.Any(x=>x.Id==key))return new(string.Empty,[],null,null,new Dictionary<string,string>());
        throw new DomainException($"Narrative reference '{key}' is missing for locale '{selected}'.");
    }

    private static (IReadOnlyList<ProposalItemView> Incoming,IReadOnlyList<ProposalItemView> Outgoing,IReadOnlyList<AgreementView> Agreements,IReadOnlyList<AvailableInteractionView> Interactions,IReadOnlyList<ScheduledConsequenceView> Consequences) Phase3(Team team,IReadOnlyList<Proposal> proposals,IReadOnlyList<ProposalRevision> revisions,IReadOnlyList<Agreement> agreements,IReadOnlyList<ScheduledConsequence> consequences,long version,StoryPackage? package,IReadOnlyList<Team> teams,IReadOnlyList<WorldEntity> entities)
    {
        ProposalItemView Item(Proposal p)
        {
            var revision=revisions.Single(x=>x.ProposalId==p.ProposalId&&x.RevisionNumber==p.CurrentRevisionNumber);var creator=revision.CreatedByTeamId;var responder=creator==p.SenderTeamId?p.ReceiverTeamId:p.SenderTeamId;var actions=new List<string>();
            if(p.Status is ProposalStatus.Pending or ProposalStatus.Countered){if(team.Id==creator)actions.Add("Cancel");if(team.Id==responder)actions.AddRange(["Accept","Counter","Reject"]);}
            var history=revisions.Where(x=>x.ProposalId==p.ProposalId).OrderBy(x=>x.RevisionNumber).Select(x=>new ProposalRevisionView(x.RevisionNumber,x.CreatedByTeamId,x.TermsPayload.Clone(),x.CreatedAtUtc)).ToList();
            return new(p.ProposalId,p.InteractionTypeId,p.SenderTeamId,p.ReceiverTeamId,p.CurrentRevisionNumber,revision.TermsPayload.Clone(),p.Status,p.ValidUntilCheckpointId,actions,version,history);
        }
        var party=proposals.Where(x=>x.SenderTeamId==team.Id||x.ReceiverTeamId==team.Id).OrderBy(x=>x.ProposalId).ToList();
        var incoming=party.Where(p=>{var r=revisions.Single(x=>x.ProposalId==p.ProposalId&&x.RevisionNumber==p.CurrentRevisionNumber);return (r.CreatedByTeamId==p.SenderTeamId?p.ReceiverTeamId:p.SenderTeamId)==team.Id;}).Select(Item).ToList();
        var outgoing=party.Except(incoming.Select(x=>proposals.Single(p=>p.ProposalId==x.ProposalId))).Select(Item).ToList();
        var visibleAgreements=agreements.Where(x=>x.PartyTeamIds.Contains(team.Id)).OrderBy(x=>x.AgreementId).Select(ToAgreementView).ToList();
        var visibleConsequences=consequences.Where(x=>x.Status==ScheduledConsequenceStatus.Pending&&(x.Visibility==ConsequenceVisibility.Public||x.Visibility==ConsequenceVisibility.PrivateToSourceTeam&&x.SourceTeamId==team.Id)).OrderBy(x=>x.ScheduledConsequenceId).Select(ToConsequenceView).ToList();
        var available=package?.InteractionDefinitions.Where(x=>SelectorMatches(team,x.AllowedSenderSelectors,entities)).OrderBy(x=>x.Id,StringComparer.Ordinal).Select(x=>new AvailableInteractionView(x.Id,teams.Where(t=>t.Id!=team.Id&&SelectorMatches(t,x.AllowedReceiverSelectors,entities)).Select(t=>t.Id).OrderBy(id=>id).ToList())).ToList()??[];
        return(incoming,outgoing,visibleAgreements,available,visibleConsequences);
    }
    private static bool SelectorMatches(Team team,IReadOnlyList<TargetSelectorDefinition> selectors,IReadOnlyList<WorldEntity> entities)=>selectors.Any(s=>s.Type==TargetSelectorType.AllTeams||s.Type==TargetSelectorType.TeamControllingEntityDefinition&&team.ControlledEntityId is { } id&&entities.Any(e=>e.Id==id&&e.DefinitionId==s.EntityDefinitionId));
    internal static AgreementView ToAgreementView(Agreement x)=>new(x.AgreementId,x.InteractionTypeId,x.PartyTeamIds,x.TermsPayload.Clone(),x.Status,x.ActivatedAtCheckpointId,x.ExecutedAtCheckpointId,x.Visibility);
    internal static ScheduledConsequenceView ToConsequenceView(ScheduledConsequence x)=>new(x.ScheduledConsequenceId,x.DefinitionId,x.DueCheckpointId,x.TriggerType,x.Status,x.Visibility);
}
