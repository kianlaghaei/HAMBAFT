namespace Hambaft.Domain;

public sealed record PresentationBandDefinition(
    string Id,
    decimal Minimum,
    decimal Maximum,
    string Label,
    string Description,
    IReadOnlyList<string> VisualTags);

public sealed record MetricPresentationDefinition(
    string MetricKey,
    MetricScope Scope,
    IReadOnlyList<PresentationBandDefinition> Bands);

public sealed record LocationPresentationDefinition(
    string Id,
    string DisplayName,
    string ShortIdentity,
    string WhyItMatters,
    string PublicCondition,
    string WhoIsHere,
    string RecentChange,
    IReadOnlyList<string> AvailableActions,
    IReadOnlyList<string> PresentationTags,
    decimal X,
    decimal Y,
    string? EntityDefinitionId = null);

public sealed record ScenePresentationDefinition(
    string Id,
    string CheckpointId,
    string TimeOfDay,
    string TimeLabel,
    string AtmosphereLabel,
    string PublicEvent,
    string CourtyardActivity,
    string Soundscape,
    bool AvanVisible,
    IReadOnlyList<string> VisualTags,
    IReadOnlyDictionary<string,string> LocationConditions,
    IReadOnlyList<string> CharacterIds);

public sealed record AmbientEventPresentationDefinition(
    string Id,
    string Description,
    string LocationId,
    string Visibility,
    IReadOnlyList<string> RequiredVisualTags,
    IReadOnlyList<string> ForbiddenVisualTags);

public sealed record BusinessStatePresentationDefinition(
    string EntityDefinitionId,
    string MetricKey,
    IReadOnlyList<PresentationBandDefinition> Bands);

public sealed record RelationshipStatePresentationDefinition(
    string RelationshipKey,
    IReadOnlyList<PresentationBandDefinition> Bands);

public sealed record CharacterPresentationDefinition(
    string Id,
    string DisplayName,
    string LocationId,
    string WhatIsKnown,
    string LastSeen,
    string Attitude,
    string RecentStatement,
    string? PossibleInteraction,
    string Visibility,
    IReadOnlyList<string> PresentationTags);

public sealed record ChoicePresentationDefinition(
    string ChoiceId,
    string Renderer,
    string ShortTitle,
    string Action,
    string ImmediateImplication,
    string? KnownCost,
    string? KnownRisk,
    bool UnknownConsequence,
    string? RelatedLocationId,
    string? RelatedCharacterId,
    string? RelatedEntityDefinitionId,
    string? EvidenceKind);

public sealed record WorldReactionPresentationDefinition(
    string CheckpointId,
    string OutcomeLine,
    IReadOnlyList<string> LocationIds,
    IReadOnlyList<string> VisualTags);

public sealed record PresentationCatalogDefinition(
    IReadOnlyList<MetricPresentationDefinition> MetricBands,
    IReadOnlyList<LocationPresentationDefinition> Locations,
    IReadOnlyList<ScenePresentationDefinition> Scenes,
    IReadOnlyList<AmbientEventPresentationDefinition> AmbientEvents,
    IReadOnlyList<BusinessStatePresentationDefinition> BusinessStates,
    IReadOnlyList<RelationshipStatePresentationDefinition> RelationshipStates,
    IReadOnlyList<CharacterPresentationDefinition> Characters,
    IReadOnlyList<ChoicePresentationDefinition> Choices,
    IReadOnlyList<WorldReactionPresentationDefinition> Reactions)
{
    public static PresentationCatalogDefinition Empty { get; } = new([],[],[],[],[],[],[],[],[]);
}
