namespace Hambaft.Domain;

public enum SessionStatus { Created, Lobby, Running, Paused, Completed, Cancelled, Faulted }
public enum ControllerType { HumanTeam, AuthoredBehavior, System }
public enum EntityStatus { Draft, Active, Inactive }
public enum MetricScope { World, Team, Entity, Relationship }
public enum MemoryScope { World, Team, Entity, Relationship }
public enum MemoryVisibility { Public, TeamPrivate, EntityPrivate, SystemOnly }
public enum StoryletAssignmentStatus { Assigned, Responded, Resolved, Superseded }

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
