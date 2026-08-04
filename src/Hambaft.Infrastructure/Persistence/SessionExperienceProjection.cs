using Hambaft.Application;
using Hambaft.Domain;
using Marten.Events.Aggregation;

namespace Hambaft.Infrastructure.Persistence;

public sealed partial class SessionExperienceProjection : SingleStreamProjection<SessionExperienceView,Guid>
{
    public SessionExperienceProjection()=>Name="SessionExperience";
    public SessionExperienceView Create(SessionCreated e)=>new(e.Id,SessionStatus.Created,[],[],[],[],[],1,e.StoryPackageId,e.StoryVersion,e.ContentHash,null,[],[]);
    public SessionExperienceView Apply(TeamAdded e,SessionExperienceView v)=>v with { Status=v.Status==SessionStatus.Created?SessionStatus.Lobby:v.Status,Teams=v.Teams.Append(new Team(e.TeamId,v.Id,e.DisplayName,e.PairingCodeHash,null,e.Metadata.OccurredAtUtc)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(WorldEntityCreated e,SessionExperienceView v)=>v with { Entities=v.Entities.Append(new(e.EntityId,v.Id,e.DefinitionId,e.DisplayName,e.ControllerType,null,e.BehaviorProfileId,e.Status)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(EntityAssignedToTeam e,SessionExperienceView v)=>v with { Teams=v.Teams.Select(x=>x.Id==e.TeamId?x with { ControlledEntityId=e.EntityId }:x).ToList(),Entities=v.Entities.Select(x=>x.Id==e.EntityId?x with { ControlledByTeamId=e.TeamId,Status=EntityStatus.Active }:x).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(InitialMetricSet e,SessionExperienceView v)=>v with { Metrics=Upsert(v.Metrics,new(e.Scope,e.ScopeId,e.MetricKey,e.NumericValue)),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(InitialMemoryAdded e,SessionExperienceView v)=>v with { Memories=v.Memories.Append(new(e.Scope,e.ScopeId,e.Key,e.OptionalJsonValue,e.Visibility,e.Metadata.OccurredAtUtc)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(InitialRelationshipSet e,SessionExperienceView v)=>v with { Relationships=Upsert(v.Relationships,new(e.SourceEntityId,e.TargetEntityId,e.RelationshipKey,e.NumericValue)),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(SessionStarted _,SessionExperienceView v)=>v with { Status=SessionStatus.Running,Entities=v.Entities.Select(x=>x with { Status=EntityStatus.Active }).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(SessionPaused _,SessionExperienceView v)=>v with { Status=SessionStatus.Paused,StateVersion=v.StateVersion+1 }; public SessionExperienceView Apply(SessionResumed _,SessionExperienceView v)=>v with { Status=SessionStatus.Running,StateVersion=v.StateVersion+1 }; public SessionExperienceView Apply(SessionCancelled _,SessionExperienceView v)=>v with { Status=SessionStatus.Cancelled,StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(NarrativeInitialized e,SessionExperienceView v)=>v with { CurrentCheckpointId=e.CheckpointId,StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(StoryletAssigned e,SessionExperienceView v)=>v with { StoryletAssignments=v.StoryletAssignments.Append(SessionStateProjection.ToAssignment(e)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(StoryChoiceSubmitted e,SessionExperienceView v)=>v with { StoryletAssignments=SessionStateProjection.Respond(v.StoryletAssignments,e.AssignmentId),SubmittedChoices=v.SubmittedChoices.Append(SessionStateProjection.ToChoice(e)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(NarrativeCheckpointResolved e,SessionExperienceView v)=>v with { CurrentCheckpointId=e.NextCheckpointId,StoryletAssignments=SessionStateProjection.Resolve(v.StoryletAssignments,e.ResolvedAssignmentIds),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(MetricChanged e,SessionExperienceView v)=>v with { Metrics=Upsert(v.Metrics,new(e.Scope,e.ScopeId,e.MetricKey,e.NewValue)),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(MetricSet e,SessionExperienceView v)=>v with { Metrics=Upsert(v.Metrics,new(e.Scope,e.ScopeId,e.MetricKey,e.NewValue)),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(StoryMemoryAdded e,SessionExperienceView v)=>v with { Memories=v.Memories.Append(new(e.Scope,e.ScopeId,e.Key,e.OptionalJsonValue,e.Visibility,e.Metadata.OccurredAtUtc)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(StoryMemoryRemoved e,SessionExperienceView v)=>v with { Memories=v.Memories.Where(x=>!(x.Scope==e.Scope&&x.ScopeId==e.ScopeId&&x.Key==e.Key)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(RelationshipChanged e,SessionExperienceView v)=>v with { Relationships=Upsert(v.Relationships,new(e.SourceEntityId,e.TargetEntityId,e.RelationshipKey,e.NewValue)),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(WorldNarrativePublished _,SessionExperienceView v)=>v with { StateVersion=v.StateVersion+1 };
    private static IReadOnlyList<Metric> Upsert(IReadOnlyList<Metric> values,Metric value)=>values.Where(x=>!(x.Scope==value.Scope&&x.ScopeId==value.ScopeId&&x.MetricKey==value.MetricKey)).Append(value).ToList();
    private static IReadOnlyList<Relationship> Upsert(IReadOnlyList<Relationship> values,Relationship value)=>values.Where(x=>!(x.SourceEntityId==value.SourceEntityId&&x.TargetEntityId==value.TargetEntityId&&x.RelationshipKey==value.RelationshipKey)).Append(value).ToList();
}
