namespace Hambaft.Domain;

public sealed record EventMetadata(
    Guid CorrelationId,
    Guid CausationId,
    Guid CommandId,
    Guid SessionId,
    Guid? TeamId,
    DateTimeOffset OccurredAtUtc);

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
