namespace Hambaft.Domain;

using System.Text.Json;

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

public sealed record SessionCreated(Guid Id, string StoryPackageId, string StoryVersion, string ContentHash, int Seed, EventMetadata Metadata, string DifficultyId = "standard") : IDomainEvent;
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

public sealed record ProposalSent(
    Guid ProposalId, string InteractionTypeId, Guid SenderTeamId, Guid ReceiverTeamId,
    int CurrentRevisionNumber, JsonElement TermsPayload, IReadOnlyList<string> OfferedEffects,
    IReadOnlyList<string> RequestedEffects, string CreatedAtCheckpointId, string ValidUntilCheckpointId,
    long CreatedAtStreamVersion, EventMetadata Metadata) : IDomainEvent;
public sealed record ProposalCountered(
    Guid ProposalId, string InteractionTypeId, Guid SenderTeamId, Guid ReceiverTeamId,
    int CurrentRevisionNumber, Guid CreatedByTeamId, JsonElement TermsPayload,
    IReadOnlyList<string> OfferedEffects, IReadOnlyList<string> RequestedEffects,
    long CreatedAtStreamVersion, EventMetadata Metadata) : IDomainEvent;
public sealed record ProposalAccepted(
    Guid ProposalId, string InteractionTypeId, Guid SenderTeamId, Guid ReceiverTeamId,
    int CurrentRevisionNumber, Guid AgreementId, EventMetadata Metadata) : IDomainEvent;
public sealed record ProposalRejected(
    Guid ProposalId, string InteractionTypeId, Guid SenderTeamId, Guid ReceiverTeamId,
    int CurrentRevisionNumber, EventMetadata Metadata) : IDomainEvent;
public sealed record ProposalCancelled(
    Guid ProposalId, string InteractionTypeId, Guid SenderTeamId, Guid ReceiverTeamId,
    int CurrentRevisionNumber, EventMetadata Metadata) : IDomainEvent;
public sealed record ProposalExpired(
    Guid ProposalId, string InteractionTypeId, Guid SenderTeamId, Guid ReceiverTeamId,
    int CurrentRevisionNumber, EventMetadata Metadata) : IDomainEvent;
public sealed record AgreementActivated(
    Guid AgreementId, Guid ProposalId, int AcceptedRevisionNumber, string InteractionTypeId,
    IReadOnlyList<Guid> PartyTeamIds, JsonElement TermsPayload, string ActivatedAtCheckpointId,
    AgreementVisibility Visibility, EventMetadata Metadata) : IDomainEvent;
public sealed record AgreementExecuted(
    Guid AgreementId, Guid ProposalId, string InteractionTypeId, IReadOnlyList<Guid> PartyTeamIds,
    int CurrentRevisionNumber, string ExecutedAtCheckpointId, EventMetadata Metadata) : IDomainEvent;
public sealed record AgreementFailed(
    Guid AgreementId, Guid ProposalId, string InteractionTypeId, IReadOnlyList<Guid> PartyTeamIds,
    int CurrentRevisionNumber, string FailedAtCheckpointId, string ReasonCode, EventMetadata Metadata) : IDomainEvent;

public sealed record AuthoredBehaviorActionSelected(
    Guid EntityId, string BehaviorProfileId, string? RuleId, string ActionId,
    string DifficultyId, int ResolutionNumber, EventMetadata Metadata) : IDomainEvent;

public sealed record ConsequenceScheduled(
    Guid ScheduledConsequenceId, string DefinitionId, Guid SourceEventId, Guid? SourceTeamId,
    Guid? SourceEntityId, string ScheduledAtCheckpointId, string? DueCheckpointId,
    ConsequenceTriggerType TriggerType, ConsequenceVisibility Visibility, EventMetadata Metadata) : IDomainEvent;
public sealed record ConsequenceTriggered(
    Guid ScheduledConsequenceId, string DefinitionId, Guid SourceEventId, Guid? SourceTeamId,
    Guid? SourceEntityId, string TriggeredAtCheckpointId, ConsequenceVisibility Visibility,
    EventMetadata Metadata) : IDomainEvent;
public sealed record ConsequenceCancelled(
    Guid ScheduledConsequenceId, string DefinitionId, Guid SourceEventId, Guid? SourceTeamId,
    Guid? SourceEntityId, EventMetadata Metadata) : IDomainEvent;
public sealed record ConsequenceFailed(
    Guid ScheduledConsequenceId, string DefinitionId, Guid SourceEventId, Guid? SourceTeamId,
    Guid? SourceEntityId, string ReasonCode, EventMetadata Metadata) : IDomainEvent;

public sealed record EntityEndingResolved(EndingResult Result, EventMetadata Metadata) : IDomainEvent;
public sealed record WorldEndingResolved(EndingResult Result, EventMetadata Metadata) : IDomainEvent;
public sealed record SessionCompleted(EventMetadata Metadata) : IDomainEvent;
