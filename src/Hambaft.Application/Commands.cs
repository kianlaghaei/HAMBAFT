using Hambaft.Domain;

namespace Hambaft.Application;

public sealed record CommandContext(Guid CorrelationId, Guid CausationId, Guid CommandId, Guid? TeamId, DateTimeOffset OccurredAtUtc)
{
    public EventMetadata For(Guid sessionId) => new(CorrelationId,CausationId,CommandId,sessionId,TeamId,OccurredAtUtc);
}

public sealed record CreateSession(Guid SessionId, string StoryPackageId, string StoryVersion, string ContentHash, int Seed, long ExpectedVersion, CommandContext Context);
public sealed record AddTeam(Guid SessionId, Guid TeamId, string DisplayName, long ExpectedVersion, CommandContext Context);
public sealed record CreateWorldEntity(Guid SessionId, Guid EntityId, string DefinitionId, string DisplayName, ControllerType ControllerType, string? BehaviorProfileId, long ExpectedVersion, CommandContext Context);
public sealed record AssignEntityToTeam(Guid SessionId, Guid EntityId, Guid TeamId, long ExpectedVersion, CommandContext Context);
public sealed record SetInitialMetric(Guid SessionId, MetricScope Scope, Guid ScopeId, string MetricKey, decimal NumericValue, long ExpectedVersion, CommandContext Context);
public sealed record AddInitialMemory(Guid SessionId, MemoryScope Scope, Guid ScopeId, string Key, string? OptionalJsonValue, MemoryVisibility Visibility, long ExpectedVersion, CommandContext Context);
public sealed record SetInitialRelationship(Guid SessionId, Guid SourceEntityId, Guid TargetEntityId, string RelationshipKey, decimal NumericValue, long ExpectedVersion, CommandContext Context);
public sealed record StartSession(Guid SessionId, long ExpectedVersion, CommandContext Context);
public sealed record PauseSession(Guid SessionId, long ExpectedVersion, CommandContext Context);
public sealed record ResumeSession(Guid SessionId, long ExpectedVersion, CommandContext Context);
public sealed record CancelSession(Guid SessionId, long ExpectedVersion, CommandContext Context);
public sealed record InitializeNarrative(Guid SessionId,long ExpectedVersion,CommandContext Context);
public sealed record SubmitStoryChoice(Guid SessionId,Guid AssignmentId,string ChoiceId,long ExpectedVersion,CommandContext Context);
public sealed record ResolveNarrativeCheckpoint(Guid SessionId,long ExpectedVersion,CommandContext Context);
public sealed record CommandResult(Guid SessionId, long StateVersion, string EventType, string? PairingCode = null);

public sealed class SessionNotFoundException(Guid id) : Exception($"Session {id:D} was not found.");
public sealed class ConcurrencyConflictException(Guid id, long expected, long actual) : Exception($"Session {id:D} expected stream version {expected}, but current version is {actual}.")
{
    public long ExpectedVersion { get; }=expected;
    public long ActualVersion { get; }=actual;
}
