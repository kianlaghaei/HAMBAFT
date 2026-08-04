namespace Hambaft.Domain;

using System.Text.Json;

public enum SessionStatus { Created, Lobby, Running, Paused, Completed, Cancelled, Faulted }
public enum ControllerType { HumanTeam, AuthoredBehavior, System }
public enum EntityStatus { Draft, Active, Inactive }
public enum MetricScope { World, Team, Entity, Relationship }
public enum MemoryScope { World, Team, Entity, Relationship }
public enum MemoryVisibility { Public, TeamPrivate, EntityPrivate, SystemOnly }
public enum StoryletAssignmentStatus { Assigned, Responded, Resolved, Superseded }
public enum ProposalStatus { Pending, Countered, Accepted, Rejected, Expired, Cancelled, Executed, Failed }
public enum AgreementStatus { Active, Executed, Failed, Cancelled }
public enum ScheduledConsequenceStatus { Pending, Triggered, Cancelled, Failed }
public enum EndingScope { Entity, World }
public enum EndingEvidenceKind { DomainEvent, Choice, Proposal, Agreement, Memory, MetricSnapshot, Relationship, BehaviorAction, Consequence }

public sealed record Team(
    Guid Id,
    Guid SessionId,
    string DisplayName,
    string PairingCodeHash,
    Guid? ControlledEntityId,
    DateTimeOffset JoinedAtUtc);

public sealed record WorldEntity(
    Guid Id,
    Guid SessionId,
    string DefinitionId,
    string DisplayName,
    ControllerType ControllerType,
    Guid? ControlledByTeamId,
    string? BehaviorProfileId,
    EntityStatus Status);

public sealed record Metric(MetricScope Scope, Guid ScopeId, string MetricKey, decimal NumericValue);

public sealed record StoryMemory(
    MemoryScope Scope,
    Guid ScopeId,
    string Key,
    string? OptionalJsonValue,
    MemoryVisibility Visibility,
    DateTimeOffset CreatedAtUtc);

public sealed record Relationship(
    Guid SourceEntityId,
    Guid TargetEntityId,
    string RelationshipKey,
    decimal NumericValue);

public sealed record StoryletAssignment(
    Guid AssignmentId,
    string StoryletId,
    string CheckpointId,
    StoryletScope Scope,
    Guid? TargetTeamId,
    Guid? TargetEntityId,
    bool RequiredResponse,
    long AssignedAtVersion,
    StoryletAssignmentStatus Status);

public sealed record SubmittedStoryChoice(
    Guid AssignmentId,
    Guid TeamId,
    string ChoiceId,
    DateTimeOffset SubmittedAtUtc,
    long SubmittedAtStreamVersion,
    Guid CommandId);

public sealed record Proposal(
    Guid ProposalId,
    Guid SessionId,
    string InteractionTypeId,
    Guid SenderTeamId,
    Guid ReceiverTeamId,
    int CurrentRevisionNumber,
    ProposalStatus Status,
    string CreatedAtCheckpointId,
    string ValidUntilCheckpointId,
    long CreatedAtStreamVersion,
    int? AcceptedRevisionNumber = null,
    Guid? AgreementId = null);

public sealed record ProposalRevision(
    Guid ProposalId,
    int RevisionNumber,
    Guid CreatedByTeamId,
    JsonElement TermsPayload,
    IReadOnlyList<string> OfferedEffects,
    IReadOnlyList<string> RequestedEffects,
    DateTimeOffset CreatedAtUtc,
    long CreatedAtStreamVersion);

public sealed record Agreement(
    Guid AgreementId,
    Guid ProposalId,
    int AcceptedRevisionNumber,
    string InteractionTypeId,
    IReadOnlyList<Guid> PartyTeamIds,
    JsonElement TermsPayload,
    AgreementStatus Status,
    string ActivatedAtCheckpointId,
    string? ExecutedAtCheckpointId = null,
    string? FailedAtCheckpointId = null,
    AgreementVisibility Visibility = AgreementVisibility.PrivateToParties);

public sealed record ScheduledConsequence(
    Guid ScheduledConsequenceId,
    string DefinitionId,
    Guid SourceEventId,
    Guid? SourceTeamId,
    Guid? SourceEntityId,
    string ScheduledAtCheckpointId,
    string? DueCheckpointId,
    ConsequenceTriggerType TriggerType,
    ScheduledConsequenceStatus Status,
    ConsequenceVisibility Visibility);

public sealed record AuthoredBehaviorSelection(
    Guid EntityId,
    string BehaviorProfileId,
    string? RuleId,
    string ActionId,
    string DifficultyId,
    string CheckpointId,
    int ResolutionNumber);

public sealed record EndingEvidence(EndingEvidenceKind Kind, Guid? ReferenceId, string? StableId, string? Key, string? Value, bool IsPublic);
public sealed record EndingMetricSnapshot(MetricScope Scope, Guid ScopeId, string MetricKey, decimal NumericValue);
public sealed record EndingResult(
    Guid EndingResultId, Guid SessionId, EndingScope Scope, Guid ScopeId, string EndingDefinitionId,
    string PackageId, string PackageVersion, string ContentHash, long ResolvedAtStreamVersion,
    string InputFingerprint, string NarrativeRef, IReadOnlyDictionary<string,string> PresentationTags,
    DateTimeOffset ResolvedAtUtc, IReadOnlyList<EndingEvidence> Evidence,
    IReadOnlyList<EndingMetricSnapshot> MetricSnapshot, string? PublicSummaryNarrativeRef = null);

public static class DomainKeys
{
    public static string Normalize(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException($"{name} is required.");
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 100) throw new DomainException($"{name} cannot exceed 100 characters.");
        if (normalized.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_')))
            throw new DomainException($"{name} may contain only ASCII letters, digits, dot, hyphen and underscore.");
        return normalized;
    }
}
