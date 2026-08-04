using Hambaft.Application;
using Hambaft.Domain;
using Marten.Events.Aggregation;

namespace Hambaft.Infrastructure.Persistence;

public sealed partial class SessionStateProjection : SingleStreamProjection<SessionStateView,Guid>
{
    public SessionStateProjection()=>Name="SessionState";
    public SessionStateView Create(SessionCreated e)=>new(e.Id,e.StoryPackageId,e.StoryVersion,e.ContentHash,e.Seed,SessionStatus.Created,e.Metadata.OccurredAtUtc,null,null,[],[],1,null,false,[],[]);
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
    public SessionStateView Apply(NarrativeCheckpointResolved e,SessionStateView v)=>v with { CurrentCheckpointId=e.NextCheckpointId,StoryletAssignments=Resolve(v.StoryletAssignments,e.ResolvedAssignmentIds),StateVersion=v.StateVersion+1 };
    public SessionStateView Apply(InitialMetricSet _,SessionStateView v)=>Bump(v); public SessionStateView Apply(InitialMemoryAdded _,SessionStateView v)=>Bump(v); public SessionStateView Apply(InitialRelationshipSet _,SessionStateView v)=>Bump(v);
    public SessionStateView Apply(MetricChanged _,SessionStateView v)=>Bump(v); public SessionStateView Apply(MetricSet _,SessionStateView v)=>Bump(v); public SessionStateView Apply(StoryMemoryAdded _,SessionStateView v)=>Bump(v); public SessionStateView Apply(StoryMemoryRemoved _,SessionStateView v)=>Bump(v); public SessionStateView Apply(RelationshipChanged _,SessionStateView v)=>Bump(v); public SessionStateView Apply(WorldNarrativePublished _,SessionStateView v)=>Bump(v);
    private static SessionStateView Bump(SessionStateView v)=>v with { StateVersion=v.StateVersion+1 };
    internal static StoryletAssignment ToAssignment(StoryletAssigned e)=>new(e.AssignmentId,e.StoryletId,e.CheckpointId,e.Scope,e.TargetTeamId,e.TargetEntityId,e.RequiredResponse,e.AssignedAtVersion,StoryletAssignmentStatus.Assigned);
    internal static SubmittedStoryChoice ToChoice(StoryChoiceSubmitted e)=>new(e.AssignmentId,e.TeamId,e.ChoiceId,e.SubmittedAtUtc,e.SubmittedAtStreamVersion,e.Metadata.CommandId);
    internal static IReadOnlyList<StoryletAssignment> Respond(IReadOnlyList<StoryletAssignment> values,Guid id)=>values.Select(x=>x.AssignmentId==id?x with { Status=StoryletAssignmentStatus.Responded }:x).ToList();
    internal static IReadOnlyList<StoryletAssignment> Resolve(IReadOnlyList<StoryletAssignment> values,IReadOnlyList<Guid> ids)=>values.Select(x=>ids.Contains(x.AssignmentId)?x with { Status=StoryletAssignmentStatus.Resolved }:x).ToList();
}

public sealed partial class PublicWorldProjection : SingleStreamProjection<PublicWorldView,Guid>
{
    public PublicWorldProjection()=>Name="PublicWorld";
    public PublicWorldView Create(SessionCreated e)=>new(e.Id,SessionStatus.Created,[],[],[],[],1,e.StoryPackageId,e.StoryVersion,e.ContentHash,null,null,null,null);
    public PublicWorldView Apply(TeamAdded _,PublicWorldView v)=>v with { Status=v.Status==SessionStatus.Created?SessionStatus.Lobby:v.Status,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(WorldEntityCreated e,PublicWorldView v)=>v with { Entities=v.Entities.Append(new WorldEntity(e.EntityId,v.Id,e.DefinitionId,e.DisplayName,e.ControllerType,null,e.BehaviorProfileId,e.Status)).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(EntityAssignedToTeam e,PublicWorldView v)=>v with { Entities=v.Entities.Select(x=>x.Id==e.EntityId?x with { ControlledByTeamId=e.TeamId,Status=EntityStatus.Active }:x).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(InitialMetricSet e,PublicWorldView v)=>e.Scope==MetricScope.World?v with { WorldMetrics=Upsert(v.WorldMetrics,new(e.Scope,e.ScopeId,e.MetricKey,e.NumericValue)),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(InitialMemoryAdded e,PublicWorldView v)=>e.Visibility==MemoryVisibility.Public?v with { PublicMemories=v.PublicMemories.Append(new StoryMemory(e.Scope,e.ScopeId,e.Key,e.OptionalJsonValue,e.Visibility,e.Metadata.OccurredAtUtc)).ToList(),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(InitialRelationshipSet e,PublicWorldView v)=>v with { PublicRelationships=Upsert(v.PublicRelationships,new(e.SourceEntityId,e.TargetEntityId,e.RelationshipKey,e.NumericValue)),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(SessionStarted _,PublicWorldView v)=>v with { Status=SessionStatus.Running,Entities=v.Entities.Select(x=>x with { Status=EntityStatus.Active }).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(SessionPaused _,PublicWorldView v)=>v with { Status=SessionStatus.Paused,StateVersion=v.StateVersion+1 }; public PublicWorldView Apply(SessionResumed _,PublicWorldView v)=>v with { Status=SessionStatus.Running,StateVersion=v.StateVersion+1 }; public PublicWorldView Apply(SessionCancelled _,PublicWorldView v)=>v with { Status=SessionStatus.Cancelled,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(NarrativeInitialized e,PublicWorldView v)=>v with { CurrentCheckpointId=e.CheckpointId,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(NarrativeCheckpointResolved e,PublicWorldView v)=>v with { CurrentCheckpointId=e.NextCheckpointId,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(MetricChanged e,PublicWorldView v)=>e.Scope==MetricScope.World?v with { WorldMetrics=Upsert(v.WorldMetrics,new(e.Scope,e.ScopeId,e.MetricKey,e.NewValue)),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(MetricSet e,PublicWorldView v)=>e.Scope==MetricScope.World?v with { WorldMetrics=Upsert(v.WorldMetrics,new(e.Scope,e.ScopeId,e.MetricKey,e.NewValue)),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(StoryMemoryAdded e,PublicWorldView v)=>e.Visibility==MemoryVisibility.Public?v with { PublicMemories=v.PublicMemories.Append(new(e.Scope,e.ScopeId,e.Key,e.OptionalJsonValue,e.Visibility,e.Metadata.OccurredAtUtc)).ToList(),StateVersion=v.StateVersion+1 }:Bump(v);
    public PublicWorldView Apply(StoryMemoryRemoved e,PublicWorldView v)=>v with { PublicMemories=v.PublicMemories.Where(x=>!(x.Scope==e.Scope&&x.ScopeId==e.ScopeId&&x.Key==e.Key)).ToList(),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(RelationshipChanged e,PublicWorldView v)=>v with { PublicRelationships=Upsert(v.PublicRelationships,new(e.SourceEntityId,e.TargetEntityId,e.RelationshipKey,e.NewValue)),StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(WorldNarrativePublished e,PublicWorldView v)=>v with { CurrentWorldStoryletId=e.StoryletId,CurrentWorldNarrativeRef=e.NarrativeRef,StateVersion=v.StateVersion+1 };
    public PublicWorldView Apply(StoryletAssigned _,PublicWorldView v)=>Bump(v); public PublicWorldView Apply(StoryChoiceSubmitted _,PublicWorldView v)=>Bump(v);
    private static PublicWorldView Bump(PublicWorldView v)=>v with { StateVersion=v.StateVersion+1 };
    private static IReadOnlyList<Metric> Upsert(IReadOnlyList<Metric> values,Metric value)=>values.Where(x=>!(x.Scope==value.Scope&&x.ScopeId==value.ScopeId&&x.MetricKey==value.MetricKey)).Append(value).ToList();
    private static IReadOnlyList<Relationship> Upsert(IReadOnlyList<Relationship> values,Relationship value)=>values.Where(x=>!(x.SourceEntityId==value.SourceEntityId&&x.TargetEntityId==value.TargetEntityId&&x.RelationshipKey==value.RelationshipKey)).Append(value).ToList();
}
