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
    AssignStorylet, PublishWorldNarrative, ScheduleConsequence, CancelConsequence
}
public enum InteractionTermType { Numeric, Boolean, ShortText, Compound }
public enum InteractionExecutionMode { ImmediateOnAcceptance, ManualExecution, ExecuteAtCheckpointResolution }
public enum ProposalValidityType { ValidUntilCheckpoint, ValidForCheckpointCount }
public enum AgreementVisibility { PrivateToParties, Public }
public enum ConsequenceTriggerType { AfterCheckpointCount, AtCheckpoint, WhenAgreementExecuted, WhenMemoryExistsAtResolution }
public enum ConsequenceVisibility { SystemOnly, PrivateToSourceTeam, Public }

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
    string? StoryletId = null,
    string? ConsequenceDefinitionId = null,
    string? ScheduledConsequenceId = null);

public sealed record InteractionTermSchemaDefinition(
    InteractionTermType Type,
    string? Name = null,
    bool Required = true,
    decimal? Minimum = null,
    decimal? Maximum = null,
    int? MaximumLength = null,
    IReadOnlyList<InteractionTermSchemaDefinition>? Fields = null);

public sealed record ProposalValidityDefinition(
    ProposalValidityType Type,
    string? CheckpointId = null,
    int? CheckpointCount = null);

public sealed record InteractionTypeDefinition(
    string Id,
    string DisplayNameRef,
    string DescriptionRef,
    IReadOnlyList<TargetSelectorDefinition> AllowedSenderSelectors,
    IReadOnlyList<TargetSelectorDefinition> AllowedReceiverSelectors,
    InteractionTermSchemaDefinition TermsSchema,
    IReadOnlyList<string> OfferedEffectIds,
    IReadOnlyList<string> RequestedEffectIds,
    ProposalValidityDefinition DefaultValidity,
    InteractionExecutionMode ExecutionMode,
    AgreementVisibility AgreementVisibility = AgreementVisibility.PrivateToParties);

public sealed record BehaviorRuleWeightModifier(string RuleId, decimal Multiplier);
public sealed record BehaviorDifficultyModifier(string DifficultyId, decimal Multiplier);
public sealed record DifficultyDefinition(
    string Id,
    string DisplayNameRef,
    IReadOnlyList<BehaviorRuleWeightModifier> BehaviorRuleWeightModifiers,
    IReadOnlyDictionary<string, decimal> ProposalStrictnessValues,
    IReadOnlyDictionary<string, decimal> ResourceThresholdModifiers,
    string? FallbackActionPreference = null);

public sealed record BehaviorRuleDefinition(
    string Id,
    int Priority,
    int Weight,
    IReadOnlyList<ConditionDefinition> Conditions,
    string ActionId,
    IReadOnlyList<BehaviorDifficultyModifier> DifficultyModifiers,
    RepeatPolicy RepeatPolicy);

public sealed record BehaviorProfileDefinition(
    string Id,
    string DisplayNameRef,
    IReadOnlyList<string> EligibleEntityDefinitions,
    IReadOnlyList<BehaviorRuleDefinition> Rules,
    string FallbackActionId);

public sealed record BehaviorInteractionActionDefinition(
    string InteractionTypeId,
    string ReceiverEntityDefinitionId,
    string TermsJson);

public sealed record BehaviorActionDefinition(
    string Id,
    IReadOnlyList<string> EffectIds,
    BehaviorInteractionActionDefinition? InteractionAction = null,
    string? NarrativeRef = null,
    string? SystemActionId = null);

public sealed record BehaviorCatalogDefinition(
    IReadOnlyList<BehaviorProfileDefinition> Profiles,
    IReadOnlyList<BehaviorActionDefinition> Actions);

public sealed record ConsequenceTriggerDefinition(
    ConsequenceTriggerType Type,
    int? CheckpointCount = null,
    string? CheckpointId = null,
    string? AgreementInteractionTypeId = null,
    string? MemoryKey = null,
    ConditionTargetScope? MemoryScope = null);

public sealed record ConsequenceDefinition(
    string Id,
    ConsequenceTriggerDefinition Trigger,
    IReadOnlyList<ConditionDefinition> Conditions,
    IReadOnlyList<string> EffectIds,
    string? NarrativeRef,
    ConsequenceVisibility Visibility,
    RepeatPolicy RepeatPolicy);

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
    string ContentHash,
    IReadOnlyList<InteractionTypeDefinition>? Interactions = null,
    BehaviorCatalogDefinition? Behaviors = null,
    IReadOnlyList<ConsequenceDefinition>? Consequences = null,
    IReadOnlyList<DifficultyDefinition>? Difficulties = null)
{
    public IReadOnlyList<InteractionTypeDefinition> InteractionDefinitions => Interactions ?? [];
    public BehaviorCatalogDefinition BehaviorDefinitions => Behaviors ?? new([], []);
    public IReadOnlyList<ConsequenceDefinition> ConsequenceDefinitions => Consequences ?? [];
    public IReadOnlyList<DifficultyDefinition> DifficultyDefinitions => Difficulties ?? [];
}

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
