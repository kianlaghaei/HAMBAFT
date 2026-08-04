namespace Hambaft.Domain;

public enum ControllerRequirement { HumanTeam, AuthoredBehavior, System, Any }
public enum StoryletScope { WorldPublic, TeamPrivate, EntityPrivate, AdminOnly }
public enum TargetSelectorType { AllTeams, TeamControllingEntityDefinition, EntityDefinition, World }
public enum RepeatPolicy { OncePerSession, OncePerCheckpoint, Repeatable }
public enum ConditionType
{
    MetricAbove, MetricAtLeast, MetricBelow, MetricAtMost, MetricEquals,
    MemoryExists, MemoryMissing, RelationshipAbove, RelationshipBelow,
    EntityControllerIs, EntityDefinitionIs, TeamControlsEntityDefinition,
    SessionStatusIs, CheckpointIs, ChoiceWasSubmitted, All, Any, Not
}
public enum ConditionTargetScope
{
    World, CurrentTeam, CurrentEntity, ExplicitEntityDefinition,
    RelationshipBetweenCurrentAndTarget
}
public enum EffectType
{
    ChangeMetric, SetMetric, AddMemory, RemoveMemory, ChangeRelationship,
    AssignStorylet, PublishWorldNarrative
}

public sealed record StoryPackageManifest(
    string Id,
    string Version,
    string Title,
    string Description,
    int MinimumTeams,
    int MaximumTeams,
    string EntryCheckpointId,
    string DefaultLocale,
    IReadOnlyList<string> SupportedLocales,
    int EstimatedDurationMinutes,
    string RequiredRuntimeVersion);

public sealed record MetricDefinition(string Key, MetricScope Scope, decimal Minimum, decimal Maximum, decimal DefaultValue);

public sealed record EntityDefinition(
    string Id,
    string DisplayName,
    ControllerRequirement ControllerRequirement,
    IReadOnlyList<string> PublicTags,
    IReadOnlyDictionary<string, decimal> InitialMetrics);

public sealed record TargetSelectorDefinition(TargetSelectorType Type, string? EntityDefinitionId = null);

public sealed record ConditionDefinition(
    ConditionType Type,
    ConditionTargetScope? Scope = null,
    string? MetricKey = null,
    decimal? Expected = null,
    string? MemoryKey = null,
    string? EntityDefinitionId = null,
    string? TargetEntityDefinitionId = null,
    string? RelationshipKey = null,
    ControllerType? Controller = null,
    SessionStatus? SessionStatus = null,
    string? CheckpointId = null,
    string? ChoiceId = null,
    IReadOnlyList<ConditionDefinition>? Conditions = null,
    ConditionDefinition? Condition = null);

public sealed record ChoiceDefinition(
    string Id,
    string LabelRef,
    IReadOnlyList<string> EffectIds,
    string? NextStoryletHint = null);

public sealed record StoryletDefinition(
    string Id,
    string CheckpointId,
    StoryletScope Scope,
    TargetSelectorDefinition TargetSelector,
    string NarrativeRef,
    IReadOnlyList<ConditionDefinition> Conditions,
    IReadOnlyList<ChoiceDefinition> Choices,
    int Priority,
    int Weight,
    RepeatPolicy RepeatPolicy,
    bool RequiredResponse,
    string? NextCheckpointId = null);

public sealed record EffectDefinition(
    string Id,
    EffectType Type,
    ConditionTargetScope? Scope = null,
    string? MetricKey = null,
    decimal? Value = null,
    string? MemoryKey = null,
    string? JsonValue = null,
    MemoryVisibility? Visibility = null,
    string? TargetEntityDefinitionId = null,
    string? RelationshipKey = null,
    string? StoryletId = null);

public sealed record NarrativeDefinition(
    string Title,
    IReadOnlyList<string> Paragraphs,
    string? ChoiceLabel,
    string? ShortOutcome,
    IReadOnlyDictionary<string, string> PresentationTags);

public sealed record StoryPackage(
    StoryPackageManifest Manifest,
    IReadOnlyList<MetricDefinition> Metrics,
    IReadOnlyList<EntityDefinition> Entities,
    IReadOnlyList<StoryletDefinition> Storylets,
    IReadOnlyList<EffectDefinition> Effects,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, NarrativeDefinition>> Narrative,
    string ContentHash);

public sealed record StoryPackageValidationError(string FilePath, string? ContentId, string ErrorCode, string Message);
public sealed record StoryPackageValidationResult(IReadOnlyList<StoryPackageValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed record StoryPackageMetadata(
    string Id,
    string Version,
    string Title,
    string Description,
    int MinimumTeams,
    int MaximumTeams,
    int EstimatedDurationMinutes,
    bool IsValid,
    string ContentHash,
    IReadOnlyList<StoryPackageValidationError> Errors);
