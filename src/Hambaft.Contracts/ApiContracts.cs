namespace Hambaft.Contracts;

public sealed record CreateSessionRequest(string StoryPackageId,string StoryVersion,string ContentHash,int Seed,long ExpectedVersion=0);
public sealed record AddTeamRequest(string DisplayName,long ExpectedVersion);
public sealed record CreateEntityRequest(string DefinitionId,string DisplayName,string ControllerType,string? BehaviorProfileId,long ExpectedVersion);
public sealed record AssignEntityRequest(Guid EntityId,Guid TeamId,long ExpectedVersion);
public sealed record TransitionRequest(long ExpectedVersion);
public sealed record MetricInput(string Scope,Guid ScopeId,string MetricKey,decimal NumericValue);
public sealed record MemoryInput(string Scope,Guid ScopeId,string Key,string? OptionalJsonValue,string Visibility);
public sealed record RelationshipInput(Guid SourceEntityId,Guid TargetEntityId,string RelationshipKey,decimal NumericValue);
public sealed record BootstrapRequest(long ExpectedVersion,IReadOnlyList<MetricInput> Metrics,IReadOnlyList<MemoryInput> Memories,IReadOnlyList<RelationshipInput> Relationships);
public sealed record CommandResponse(Guid SessionId,long StateVersion,string EventType,Guid? ResourceId=null,string? PairingCode=null);
public sealed record StateChangedNotification(Guid SessionId,long StateVersion,string EventType,string Scope);
