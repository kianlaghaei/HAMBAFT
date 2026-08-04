using Hambaft.Application;
using Hambaft.Domain;
using Marten.Events.Aggregation;

namespace Hambaft.Infrastructure.Persistence;

public sealed class SessionStateProjection : SingleStreamProjection<SessionStateView,Guid>
{
    public SessionStateView Create(SessionCreated e)=>new(e.Id,e.StoryPackageId,e.StoryVersion,e.ContentHash,e.Seed,SessionStatus.Created,e.Metadata.OccurredAtUtc,null,null,[],[],1);
    public SessionStateView Apply(TeamAdded e,SessionStateView v)=>v with { Status=v.Status==SessionStatus.Created?SessionStatus.Lobby:v.Status,Teams=v.Teams.Append(new Team(e.TeamId,v.Id,e.DisplayName,e.PairingCodeHash,null,e.Metadata.OccurredAtUtc)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(WorldEntityCreated e,SessionStateView v)=>v with { Entities=v.Entities.Append(new WorldEntity(e.EntityId,v.Id,e.DefinitionId,e.DisplayName,e.ControllerType,null,e.BehaviorProfileId,e.Status)).ToList(),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(EntityAssignedToTeam e,SessionStateView v)=>v with { Teams=v.Teams.Select(x=>x.Id==e.TeamId?x with { ControlledEntityId=e.EntityId }:x).ToList(),Entities=v.Entities.Select(x=>x.Id==e.EntityId?x with { ControlledByTeamId=e.TeamId,Status=EntityStatus.Active }:x).ToList(),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(InitialMetricSet _,SessionStateView v)=>Bump(v);
    public SessionStateView Apply(InitialMemoryAdded _,SessionStateView v)=>Bump(v);
    public SessionStateView Apply(InitialRelationshipSet _,SessionStateView v)=>Bump(v);
    public SessionStateView Apply(SessionStarted e,SessionStateView v)=>v with { Status=SessionStatus.Running,StartedAtUtc=e.Metadata.OccurredAtUtc,Entities=v.Entities.Select(x=>x with { Status=EntityStatus.Active }).ToList(),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(SessionPaused _,SessionStateView v)=>v with { Status=SessionStatus.Paused,StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(SessionResumed _,SessionStateView v)=>v with { Status=SessionStatus.Running,StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(SessionCancelled e,SessionStateView v)=>v with { Status=SessionStatus.Cancelled,CompletedAtUtc=e.Metadata.OccurredAtUtc,StateVersion=v.StateVersion+1 };
    private static SessionStateView Bump(SessionStateView v)=>v with { StateVersion=v.StateVersion+1 };
}

public sealed class PublicWorldProjection : SingleStreamProjection<PublicWorldView,Guid>
{
    public PublicWorldView Create(SessionCreated e)=>new(e.Id,SessionStatus.Created,[],[],[],[],1);
    public PublicWorldView Apply(TeamAdded _,PublicWorldView v)=>v with { Status=v.Status==SessionStatus.Created?SessionStatus.Lobby:v.Status,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(WorldEntityCreated e,PublicWorldView v)=>v with { Entities=v.Entities.Append(new WorldEntity(e.EntityId,v.Id,e.DefinitionId,e.DisplayName,e.ControllerType,null,e.BehaviorProfileId,e.Status)).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(EntityAssignedToTeam e,PublicWorldView v)=>v with { Entities=v.Entities.Select(x=>x.Id==e.EntityId?x with { ControlledByTeamId=e.TeamId,Status=EntityStatus.Active }:x).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(InitialMetricSet e,PublicWorldView v)=>e.Scope==MetricScope.World?v with { WorldMetrics=v.WorldMetrics.Append(new Metric(e.Scope,e.ScopeId,e.MetricKey,e.NumericValue)).ToList(),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(InitialMemoryAdded e,PublicWorldView v)=>e.Visibility==MemoryVisibility.Public?v with { PublicMemories=v.PublicMemories.Append(new StoryMemory(e.Scope,e.ScopeId,e.Key,e.OptionalJsonValue,e.Visibility,e.Metadata.OccurredAtUtc)).ToList(),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(InitialRelationshipSet e,PublicWorldView v)=>v with { PublicRelationships=v.PublicRelationships.Append(new Relationship(e.SourceEntityId,e.TargetEntityId,e.RelationshipKey,e.NumericValue)).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(SessionStarted _,PublicWorldView v)=>v with { Status=SessionStatus.Running,Entities=v.Entities.Select(x=>x with { Status=EntityStatus.Active }).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(SessionPaused _,PublicWorldView v)=>v with { Status=SessionStatus.Paused,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(SessionResumed _,PublicWorldView v)=>v with { Status=SessionStatus.Running,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(SessionCancelled _,PublicWorldView v)=>v with { Status=SessionStatus.Cancelled,StateVersion=v.StateVersion+1 };
    private static PublicWorldView Bump(PublicWorldView v)=>v with { StateVersion=v.StateVersion+1 };
}
