using Hambaft.Application;
using Hambaft.Domain;
using Marten.Events.Aggregation;

namespace Hambaft.Infrastructure.Persistence;

/// <summary>
/// Replays one Session stream into the source model for Team and Entity experiences.
/// Team- and Entity-specific views are materialized from this document without imperative document writes.
/// </summary>
public sealed partial class SessionExperienceProjection : SingleStreamProjection<SessionExperienceView,Guid>
{
    public SessionExperienceProjection()=>Name="SessionExperience";

    public SessionExperienceView Create(SessionCreated e)=>new(e.Id,SessionStatus.Created,[],[],[],[],[],1);
    public SessionExperienceView Apply(TeamAdded e,SessionExperienceView v)=>v with
    {
        Status=v.Status==SessionStatus.Created?SessionStatus.Lobby:v.Status,
        Teams=v.Teams.Append(new Team(e.TeamId,v.Id,e.DisplayName,e.PairingCodeHash,null,e.Metadata.OccurredAtUtc)).ToList(),
        StateVersion=v.StateVersion+1
    };
    public SessionExperienceView Apply(WorldEntityCreated e,SessionExperienceView v)=>v with
    {
        Entities=v.Entities.Append(new WorldEntity(e.EntityId,v.Id,e.DefinitionId,e.DisplayName,e.ControllerType,null,e.BehaviorProfileId,e.Status)).ToList(),
        StateVersion=v.StateVersion+1
    };
    public SessionExperienceView Apply(EntityAssignedToTeam e,SessionExperienceView v)=>v with
    {
        Teams=v.Teams.Select(x=>x.Id==e.TeamId?x with { ControlledEntityId=e.EntityId }:x).ToList(),
        Entities=v.Entities.Select(x=>x.Id==e.EntityId?x with { ControlledByTeamId=e.TeamId,Status=EntityStatus.Active }:x).ToList(),
        StateVersion=v.StateVersion+1
    };
    public SessionExperienceView Apply(InitialMetricSet e,SessionExperienceView v)=>v with { Metrics=v.Metrics.Append(new Metric(e.Scope,e.ScopeId,e.MetricKey,e.NumericValue)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(InitialMemoryAdded e,SessionExperienceView v)=>v with { Memories=v.Memories.Append(new StoryMemory(e.Scope,e.ScopeId,e.Key,e.OptionalJsonValue,e.Visibility,e.Metadata.OccurredAtUtc)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(InitialRelationshipSet e,SessionExperienceView v)=>v with { Relationships=v.Relationships.Append(new Relationship(e.SourceEntityId,e.TargetEntityId,e.RelationshipKey,e.NumericValue)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(SessionStarted _,SessionExperienceView v)=>v with { Status=SessionStatus.Running,Entities=v.Entities.Select(x=>x with { Status=EntityStatus.Active }).ToList(),StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(SessionPaused _,SessionExperienceView v)=>v with { Status=SessionStatus.Paused,StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(SessionResumed _,SessionExperienceView v)=>v with { Status=SessionStatus.Running,StateVersion=v.StateVersion+1 };
    public SessionExperienceView Apply(SessionCancelled _,SessionExperienceView v)=>v with { Status=SessionStatus.Cancelled,StateVersion=v.StateVersion+1 };
}
