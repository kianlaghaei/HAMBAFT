using Hambaft.Application;
using Hambaft.Domain;
using Marten.Events.Aggregation;

namespace Hambaft.Infrastructure.Persistence;

public sealed partial class SessionStateProjection : SingleStreamProjection<SessionStateView,Guid>
{
    public SessionStateProjection()=>Name="SessionState";
    public SessionStateView Create(SessionCreated e)=>new(e.Id,e.StoryPackageId,e.StoryVersion,e.ContentHash,e.Seed,SessionStatus.Created,e.Metadata.OccurredAtUtc,null,null,[],[],1,null,false,[],[],e.DifficultyId,0,[],[],[],[],[],[]);
    public SessionStateView Apply(TeamAdded e,SessionStateView v)=>v with { Status=v.Status==SessionStatus.Created?SessionStatus.Lobby:v.Status,Teams=v.Teams.Append(new Team(e.TeamId,v.Id,e.DisplayName,e.PairingCodeHash,null,e.Metadata.OccurredAtUtc)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(WorldEntityCreated e,SessionStateView v)=>v with { Entities=v.Entities.Append(new WorldEntity(e.EntityId,v.Id,e.DefinitionId,e.DisplayName,e.ControllerType,null,e.BehaviorProfileId,e.Status)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(EntityAssignedToTeam e,SessionStateView v)=>v with { Teams=v.Teams.Select(x=>x.Id==e.TeamId?x with { ControlledEntityId=e.EntityId }:x).ToList(),Entities=v.Entities.Select(x=>x.Id==e.EntityId?x with { ControlledByTeamId=e.TeamId,Status=EntityStatus.Active }:x).ToList(),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(SessionStarted e,SessionStateView v)=>v with { Status=SessionStatus.Running,StartedAtUtc=e.Metadata.OccurredAtUtc,Entities=v.Entities.Select(x=>x with { Status=EntityStatus.Active }).ToList(),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(SessionPaused _,SessionStateView v)=>v with { Status=SessionStatus.Paused,StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(SessionResumed _,SessionStateView v)=>v with { Status=SessionStatus.Running,StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(SessionCancelled e,SessionStateView v)=>v with { Status=SessionStatus.Cancelled,CompletedAtUtc=e.Metadata.OccurredAtUtc,StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(NarrativeInitialized e,SessionStateView v)=>v with { NarrativeInitialized=true,CurrentCheckpointId=e.CheckpointId,StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(StoryletAssigned e,SessionStateView v)=>v with { StoryletAssignments=v.StoryletAssignments.Append(ToAssignment(e)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(StoryChoiceSubmitted e,SessionStateView v)=>v with { StoryletAssignments=Respond(v.StoryletAssignments,e.AssignmentId),SubmittedChoices=v.SubmittedChoices.Append(ToChoice(e)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(NarrativeCheckpointResolved e,SessionStateView v)=>v with { CurrentCheckpointId=e.NextCheckpointId,CheckpointResolutionNumber=v.CheckpointResolutionNumber+1,StoryletAssignments=Resolve(v.StoryletAssignments,e.ResolvedAssignmentIds),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(InitialMetricSet _,SessionStateView v)=>Bump(v); public SessionStateView Apply(InitialMemoryAdded _,SessionStateView v)=>Bump(v); public SessionStateView Apply(InitialRelationshipSet _,SessionStateView v)=>Bump(v);
    public SessionStateView Apply(MetricChanged _,SessionStateView v)=>Bump(v); public SessionStateView Apply(MetricSet _,SessionStateView v)=>Bump(v); public SessionStateView Apply(StoryMemoryAdded _,SessionStateView v)=>Bump(v); public SessionStateView Apply(StoryMemoryRemoved _,SessionStateView v)=>Bump(v); public SessionStateView Apply(RelationshipChanged _,SessionStateView v)=>Bump(v); public SessionStateView Apply(WorldNarrativePublished _,SessionStateView v)=>Bump(v);
    public SessionStateView Apply(ProposalSent e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(ProposalCountered e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(ProposalAccepted e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(ProposalRejected e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(ProposalCancelled e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(ProposalExpired e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(AgreementActivated e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(AgreementExecuted e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(AgreementFailed e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(AuthoredBehaviorActionSelected e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(ConsequenceScheduled e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(ConsequenceTriggered e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(ConsequenceCancelled e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(ConsequenceFailed e,SessionStateView v)=>Phase3Projection.Apply(v,e);
    public SessionStateView Apply(EntityEndingResolved e,SessionStateView v)=>v with { EndingResults=(v.EndingResults??[]).Append(e.Result).ToList(),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(WorldEndingResolved e,SessionStateView v)=>v with { EndingResults=(v.EndingResults??[]).Append(e.Result).ToList(),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(SessionCompleted e,SessionStateView v)=>v with { Status=SessionStatus.Completed,CompletedAtUtc=e.Metadata.OccurredAtUtc,StateVersion=v.StateVersion+1 };
    private static SessionStateView Bump(SessionStateView v)=>v with { StateVersion=v.StateVersion+1 };
    internal static StoryletAssignment ToAssignment(StoryletAssigned e)=>new(e.AssignmentId,e.StoryletId,e.CheckpointId,e.Scope,e.TargetTeamId,e.TargetEntityId,e.RequiredResponse,e.AssignedAtVersion,StoryletAssignmentStatus.Assigned);
    internal static SubmittedStoryChoice ToChoice(StoryChoiceSubmitted e)=>new(e.AssignmentId,e.TeamId,e.ChoiceId,e.SubmittedAtUtc,e.SubmittedAtStreamVersion,e.Metadata.CommandId);
    internal static IReadOnlyList<StoryletAssignment> Respond(IReadOnlyList<StoryletAssignment> values,Guid id)=>values.Select(x=>x.AssignmentId==id?x with { Status=StoryletAssignmentStatus.Responded }:x).ToList();
    internal static IReadOnlyList<StoryletAssignment> Resolve(IReadOnlyList<StoryletAssignment> values,IReadOnlyList<Guid> ids)=>values.Select(x=>ids.Contains(x.AssignmentId)?x with { Status=StoryletAssignmentStatus.Resolved }:x).ToList();
}

public sealed partial class PublicWorldProjection : SingleStreamProjection<PublicWorldView,Guid>
{
    public PublicWorldProjection()=>Name="PublicWorld";
    public PublicWorldView Create(SessionCreated e)=>new(e.Id,SessionStatus.Created,[],[],[],[],1,e.StoryPackageId,e.StoryVersion,e.ContentHash,null,null,null,null,DifficultyId:e.DifficultyId);
    public PublicWorldView Apply(TeamAdded _,PublicWorldView v)=>v with { Status=v.Status==SessionStatus.Created?SessionStatus.Lobby:v.Status,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(WorldEntityCreated e,PublicWorldView v)=>v with { Entities=v.Entities.Append(new WorldEntity(e.EntityId,v.Id,e.DefinitionId,e.DisplayName,e.ControllerType,null,e.BehaviorProfileId,e.Status)).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(EntityAssignedToTeam e,PublicWorldView v)=>v with { Entities=v.Entities.Select(x=>x.Id==e.EntityId?x with { ControlledByTeamId=e.TeamId,Status=EntityStatus.Active }:x).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(InitialMetricSet e,PublicWorldView v)=>e.Scope==MetricScope.World?v with { WorldMetrics=Upsert(v.WorldMetrics,new(e.Scope,e.ScopeId,e.MetricKey,e.NumericValue)),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(InitialMemoryAdded e,PublicWorldView v)=>e.Visibility==MemoryVisibility.Public?v with { PublicMemories=v.PublicMemories.Append(new StoryMemory(e.Scope,e.ScopeId,e.Key,e.OptionalJsonValue,e.Visibility,e.Metadata.OccurredAtUtc)).ToList(),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(InitialRelationshipSet _,PublicWorldView v)=>Bump(v);
    public PublicWorldView Apply(SessionStarted _,PublicWorldView v)=>v with { Status=SessionStatus.Running,Entities=v.Entities.Select(x=>x with { Status=EntityStatus.Active }).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(SessionPaused _,PublicWorldView v)=>v with { Status=SessionStatus.Paused,StateVersion=v.StateVersion+1 }; public PublicWorldView Apply(SessionResumed _,PublicWorldView v)=>v with { Status=SessionStatus.Running,StateVersion=v.StateVersion+1 }; public PublicWorldView Apply(SessionCancelled _,PublicWorldView v)=>v with { Status=SessionStatus.Cancelled,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(NarrativeInitialized e,PublicWorldView v)=>v with { CurrentCheckpointId=e.CheckpointId,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(NarrativeCheckpointResolved e,PublicWorldView v)=>v with { CurrentCheckpointId=e.NextCheckpointId,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(MetricChanged e,PublicWorldView v)=>e.Scope==MetricScope.World?v with { WorldMetrics=Upsert(v.WorldMetrics,new(e.Scope,e.ScopeId,e.MetricKey,e.NewValue)),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(MetricSet e,PublicWorldView v)=>e.Scope==MetricScope.World?v with { WorldMetrics=Upsert(v.WorldMetrics,new(e.Scope,e.ScopeId,e.MetricKey,e.NewValue)),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(StoryMemoryAdded e,PublicWorldView v)=>e.Visibility==MemoryVisibility.Public?v with { PublicMemories=v.PublicMemories.Append(new(e.Scope,e.ScopeId,e.Key,e.OptionalJsonValue,e.Visibility,e.Metadata.OccurredAtUtc)).ToList(),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(StoryMemoryRemoved e,PublicWorldView v)=>v with { PublicMemories=v.PublicMemories.Where(x=>!(x.Scope==e.Scope&&x.ScopeId==e.ScopeId&&x.Key==e.Key)).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(RelationshipChanged _,PublicWorldView v)=>Bump(v);
    public PublicWorldView Apply(WorldNarrativePublished e,PublicWorldView v)=>v with { CurrentWorldStoryletId=e.StoryletId,CurrentWorldNarrativeRef=e.NarrativeRef,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(StoryletAssigned _,PublicWorldView v)=>Bump(v); public PublicWorldView Apply(StoryChoiceSubmitted _,PublicWorldView v)=>Bump(v);
    public PublicWorldView Apply(ProposalSent _,PublicWorldView v)=>Bump(v);public PublicWorldView Apply(ProposalCountered _,PublicWorldView v)=>Bump(v);public PublicWorldView Apply(ProposalAccepted _,PublicWorldView v)=>Bump(v);public PublicWorldView Apply(ProposalRejected _,PublicWorldView v)=>Bump(v);public PublicWorldView Apply(ProposalCancelled _,PublicWorldView v)=>Bump(v);public PublicWorldView Apply(ProposalExpired _,PublicWorldView v)=>Bump(v);
    public PublicWorldView Apply(AgreementActivated e,PublicWorldView v)=>e.Visibility==AgreementVisibility.Public?v with { PublicAgreements=(v.PublicAgreements??[]).Append(new(e.AgreementId,e.InteractionTypeId,e.PartyTeamIds,e.TermsPayload.Clone(),AgreementStatus.Active,e.ActivatedAtCheckpointId,null,e.Visibility)).ToList(),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(AgreementExecuted e,PublicWorldView v)=>v with { PublicAgreements=(v.PublicAgreements??[]).Select(x=>x.AgreementId==e.AgreementId?x with { Status=AgreementStatus.Executed,ExecutionCheckpoint=e.ExecutedAtCheckpointId }:x).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(AgreementFailed e,PublicWorldView v)=>v with { PublicAgreements=(v.PublicAgreements??[]).Select(x=>x.AgreementId==e.AgreementId?x with { Status=AgreementStatus.Failed }:x).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(AuthoredBehaviorActionSelected _,PublicWorldView v)=>Bump(v);
    public PublicWorldView Apply(ConsequenceScheduled e,PublicWorldView v)=>e.Visibility==ConsequenceVisibility.Public?v with { PublicConsequences=(v.PublicConsequences??[]).Append(new(e.ScheduledConsequenceId,e.DefinitionId,e.DueCheckpointId,e.TriggerType,ScheduledConsequenceStatus.Pending,e.Visibility)).ToList(),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(ConsequenceTriggered e,PublicWorldView v)=>v with { PublicConsequences=(v.PublicConsequences??[]).Select(x=>x.ScheduledConsequenceId==e.ScheduledConsequenceId?x with { Status=ScheduledConsequenceStatus.Triggered }:x).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(ConsequenceCancelled e,PublicWorldView v)=>v with { PublicConsequences=(v.PublicConsequences??[]).Select(x=>x.ScheduledConsequenceId==e.ScheduledConsequenceId?x with { Status=ScheduledConsequenceStatus.Cancelled }:x).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(ConsequenceFailed e,PublicWorldView v)=>v with { PublicConsequences=(v.PublicConsequences??[]).Select(x=>x.ScheduledConsequenceId==e.ScheduledConsequenceId?x with { Status=ScheduledConsequenceStatus.Failed }:x).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(EntityEndingResolved e,PublicWorldView v)=>v with { PublicEntityEndingSummaries=e.Result.PublicSummaryNarrativeRef is null?v.PublicEntityEndingSummaries:(v.PublicEntityEndingSummaries??[]).Append(Presentation(e.Result,[])).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(WorldEndingResolved e,PublicWorldView v)=>v with { WorldEnding=Presentation(e.Result,e.Result.Evidence.Where(x=>x.IsPublic).ToList()),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(SessionCompleted _,PublicWorldView v)=>v with { Status=SessionStatus.Completed,StateVersion=v.StateVersion+1 };
    private static PublicWorldView Bump(PublicWorldView v)=>v with { StateVersion=v.StateVersion+1 };
    private static IReadOnlyList<Metric> Upsert(IReadOnlyList<Metric> values,Metric value)=>values.Where(x=>!(x.Scope==value.Scope&&x.ScopeId==value.ScopeId&&x.MetricKey==value.MetricKey)).Append(value).ToList();
    internal static EndingPresentationView Presentation(EndingResult r,IReadOnlyList<EndingEvidence> evidence)=>new(r.EndingResultId,r.Scope,r.ScopeId,r.EndingDefinitionId,string.Empty,[],r.PresentationTags,evidence,r.ContentHash,r.ResolvedAtStreamVersion);
}

public sealed partial class EndingEvidenceProjection : SingleStreamProjection<EndingEvidenceView,Guid>
{
    public EndingEvidenceProjection()=>Name="EndingEvidence";
    public EndingEvidenceView Create(SessionCreated e)=>new(e.Id,[],null,e.StoryPackageId,e.StoryVersion,e.ContentHash,1);
    public EndingEvidenceView Apply(EntityEndingResolved e,EndingEvidenceView v)=>v with { EntityEndings=v.EntityEndings.Append(e.Result).ToList(),StateVersion=e.Result.ResolvedAtStreamVersion };
    public EndingEvidenceView Apply(WorldEndingResolved e,EndingEvidenceView v)=>v with { WorldEnding=e.Result,StateVersion=e.Result.ResolvedAtStreamVersion };
    public EndingEvidenceView Apply(SessionCompleted _,EndingEvidenceView v)=>v with { StateVersion=v.StateVersion+1 };
}

internal static class Phase3Projection
{
    public static SessionStateView Apply(SessionStateView v,ProposalSent e)=>v with { Proposals=(v.Proposals??[]).Append(new(e.ProposalId,v.Id,e.InteractionTypeId,e.SenderTeamId,e.ReceiverTeamId,e.CurrentRevisionNumber,ProposalStatus.Pending,e.CreatedAtCheckpointId,e.ValidUntilCheckpointId,e.CreatedAtStreamVersion)).ToList(),ProposalRevisions=(v.ProposalRevisions??[]).Append(new ProposalRevision(e.ProposalId,e.CurrentRevisionNumber,e.SenderTeamId,e.TermsPayload.Clone(),e.OfferedEffects,e.RequestedEffects,e.Metadata.OccurredAtUtc,e.CreatedAtStreamVersion)).ToList(),StateVersion=v.StateVersion+1 };
    public static SessionStateView Apply(SessionStateView v,ProposalCountered e)=>v with { Proposals=Proposal(v.Proposals,e.ProposalId,x=>x with { CurrentRevisionNumber=e.CurrentRevisionNumber,Status=ProposalStatus.Countered }),ProposalRevisions=(v.ProposalRevisions??[]).Append(new ProposalRevision(e.ProposalId,e.CurrentRevisionNumber,e.CreatedByTeamId,e.TermsPayload.Clone(),e.OfferedEffects,e.RequestedEffects,e.Metadata.OccurredAtUtc,e.CreatedAtStreamVersion)).ToList(),StateVersion=v.StateVersion+1 };
    public static SessionStateView Apply(SessionStateView v,ProposalAccepted e)=>v with { Proposals=Proposal(v.Proposals,e.ProposalId,x=>x with { Status=ProposalStatus.Accepted,AcceptedRevisionNumber=e.CurrentRevisionNumber,AgreementId=e.AgreementId }),StateVersion=v.StateVersion+1 };
    public static SessionStateView Apply(SessionStateView v,ProposalRejected e)=>Status(v,e.ProposalId,ProposalStatus.Rejected);public static SessionStateView Apply(SessionStateView v,ProposalCancelled e)=>Status(v,e.ProposalId,ProposalStatus.Cancelled);public static SessionStateView Apply(SessionStateView v,ProposalExpired e)=>Status(v,e.ProposalId,ProposalStatus.Expired);
    public static SessionStateView Apply(SessionStateView v,AgreementActivated e)=>v with { Agreements=(v.Agreements??[]).Append(new Agreement(e.AgreementId,e.ProposalId,e.AcceptedRevisionNumber,e.InteractionTypeId,e.PartyTeamIds,e.TermsPayload.Clone(),AgreementStatus.Active,e.ActivatedAtCheckpointId,null,null,e.Visibility)).ToList(),StateVersion=v.StateVersion+1 };
    public static SessionStateView Apply(SessionStateView v,AgreementExecuted e)=>v with { Agreements=Agreement(v.Agreements,e.AgreementId,x=>x with { Status=AgreementStatus.Executed,ExecutedAtCheckpointId=e.ExecutedAtCheckpointId }),Proposals=Proposal(v.Proposals,e.ProposalId,x=>x with { Status=ProposalStatus.Executed }),StateVersion=v.StateVersion+1 };
    public static SessionStateView Apply(SessionStateView v,AgreementFailed e)=>v with { Agreements=Agreement(v.Agreements,e.AgreementId,x=>x with { Status=AgreementStatus.Failed,FailedAtCheckpointId=e.FailedAtCheckpointId }),Proposals=Proposal(v.Proposals,e.ProposalId,x=>x with { Status=ProposalStatus.Failed }),StateVersion=v.StateVersion+1 };
    public static SessionStateView Apply(SessionStateView v,AuthoredBehaviorActionSelected e)=>v with { AuthoredBehaviorSelections=(v.AuthoredBehaviorSelections??[]).Append(new AuthoredBehaviorSelection(e.EntityId,e.BehaviorProfileId,e.RuleId,e.ActionId,e.DifficultyId,e.Metadata.CheckpointId??string.Empty,e.ResolutionNumber)).ToList(),StateVersion=v.StateVersion+1 };
    public static SessionStateView Apply(SessionStateView v,ConsequenceScheduled e)=>v with { ScheduledConsequences=(v.ScheduledConsequences??[]).Append(new ScheduledConsequence(e.ScheduledConsequenceId,e.DefinitionId,e.SourceEventId,e.SourceTeamId,e.SourceEntityId,e.ScheduledAtCheckpointId,e.DueCheckpointId,e.TriggerType,ScheduledConsequenceStatus.Pending,e.Visibility)).ToList(),StateVersion=v.StateVersion+1 };
    public static SessionStateView Apply(SessionStateView v,ConsequenceTriggered e)=>Consequence(v,e.ScheduledConsequenceId,ScheduledConsequenceStatus.Triggered);public static SessionStateView Apply(SessionStateView v,ConsequenceCancelled e)=>Consequence(v,e.ScheduledConsequenceId,ScheduledConsequenceStatus.Cancelled);public static SessionStateView Apply(SessionStateView v,ConsequenceFailed e)=>Consequence(v,e.ScheduledConsequenceId,ScheduledConsequenceStatus.Failed);
    private static SessionStateView Status(SessionStateView v,Guid id,ProposalStatus status)=>v with { Proposals=Proposal(v.Proposals,id,x=>x with { Status=status }),StateVersion=v.StateVersion+1 };
    private static SessionStateView Consequence(SessionStateView v,Guid id,ScheduledConsequenceStatus status)=>v with { ScheduledConsequences=(v.ScheduledConsequences??[]).Select(x=>x.ScheduledConsequenceId==id?x with { Status=status }:x).ToList(),StateVersion=v.StateVersion+1 };
    private static IReadOnlyList<Proposal> Proposal(IReadOnlyList<Proposal>? values,Guid id,Func<Proposal,Proposal> change)=>(values??[]).Select(x=>x.ProposalId==id?change(x):x).ToList();
    private static IReadOnlyList<Agreement> Agreement(IReadOnlyList<Agreement>? values,Guid id,Func<Agreement,Agreement> change)=>(values??[]).Select(x=>x.AgreementId==id?change(x):x).ToList();
}
