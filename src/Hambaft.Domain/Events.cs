namespace Hambaft.Domain;

public sealed record EventMetadata(
    Guid CorrelationId,
    Guid CausationId,
    Guid CommandId,
    Guid SessionId,
    Guid? TeamId,
    DateTimeOffset OccurredAtUtc,
    string? CheckpointId = null,
    Guid? StoryletAssignmentId = null,
    Guid? ChoiceSubmissionId = null);

public interface IDomainEvent { EventMetadata Metadata { get; } }

public sealed record SessionCreated(Guid Id, string StoryPackageId, string StoryVersion, string ContentHash, int Seed, EventMetadata Metadata) : IDomainEvent;
public sealed record TeamAdded(Guid TeamId, string DisplayName, string PairingCodeHash, EventMetadata Metadata) : IDomainEvent;
public sealed record WorldEntityCreated(Guid EntityId, string DefinitionId, string DisplayName, ControllerType ControllerType, string? BehaviorProfileId, EntityStatus Status, EventMetadata Metadata) : IDomainEvent;
public sealed record EntityAssignedToTeam(Guid EntityId, Guid TeamId, EventMetadata Metadata) : IDomainEvent;
public sealed record InitialMetricSet(MetricScope Scope, Guid ScopeId, string MetricKey, decimal NumericValue, EventMetadata Metadata) : IDomainEvent;
public sealed record InitialMemoryAdded(MemoryScope Scope, Guid ScopeId, string Key, string? OptionalJsonValue, MemoryVisibility Visibility, EventMetadata Metadata) : IDomainEvent;
public sealed record InitialRelationshipSet(Guid SourceEntityId, Guid TargetEntityId, string RelationshipKey, decimal NumericValue, EventMetadata Metadata) : IDomainEvent;
public sealed record SessionStarted(EventMetadata Metadata) : IDomainEvent;
public sealed record SessionPaused(EventMetadata Metadata) : IDomainEvent;
public sealed record SessionResumed(EventMetadata Metadata) : IDomainEvent;
public sealed record SessionCancelled(EventMetadata Metadata) : IDomainEvent;

public sealed record NarrativeInitialized(string PackageId, string PackageVersion, string ContentHash, string CheckpointId, EventMetadata Metadata) : IDomainEvent;
public sealed record StoryletAssigned(Guid AssignmentId, string StoryletId, string CheckpointId, StoryletScope Scope, Guid? TargetTeamId, Guid? TargetEntityId, bool RequiredResponse, long AssignedAtVersion, EventMetadata Metadata) : IDomainEvent;
public sealed record StoryChoiceSubmitted(Guid AssignmentId, Guid TeamId, string ChoiceId, DateTimeOffset SubmittedAtUtc, long SubmittedAtStreamVersion, EventMetadata Metadata) : IDomainEvent;
public sealed record NarrativeCheckpointResolved(string CheckpointId, string NextCheckpointId, IReadOnlyList<Guid> ResolvedAssignmentIds, EventMetadata Metadata) : IDomainEvent;
public sealed record MetricChanged(MetricScope Scope, Guid ScopeId, string MetricKey, decimal PreviousValue, decimal NewValue, decimal RequestedDelta, string EffectId, EventMetadata Metadata) : IDomainEvent;
public sealed record MetricSet(MetricScope Scope, Guid ScopeId, string MetricKey, decimal PreviousValue, decimal NewValue, string EffectId, EventMetadata Metadata) : IDomainEvent;
public sealed record StoryMemoryAdded(MemoryScope Scope, Guid ScopeId, string Key, string? OptionalJsonValue, MemoryVisibility Visibility, string EffectId, EventMetadata Metadata) : IDomainEvent;
public sealed record StoryMemoryRemoved(MemoryScope Scope, Guid ScopeId, string Key, string EffectId, EventMetadata Metadata) : IDomainEvent;
public sealed record RelationshipChanged(Guid SourceEntityId, Guid TargetEntityId, string RelationshipKey, decimal PreviousValue, decimal NewValue, decimal RequestedDelta, string EffectId, EventMetadata Metadata) : IDomainEvent;
public sealed record WorldNarrativePublished(string StoryletId, string NarrativeRef, string CheckpointId, int Revision, string EffectId, EventMetadata Metadata) : IDomainEvent;
