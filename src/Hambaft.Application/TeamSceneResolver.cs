using Hambaft.Domain;

namespace Hambaft.Application;

public static class TeamSceneResolver
{
    public static TeamScenePresentationDefinition? Resolve(
        StoryPackage package,
        string? checkpointId,
        Team team,
        WorldEntity? entity,
        IReadOnlyList<SubmittedStoryChoice> submissions,
        IReadOnlyList<StoryMemory> memories)
    {
        if(checkpointId is null||entity is null)return null;
        var choiceIds=submissions.Where(x=>x.TeamId==team.Id).Select(x=>x.ChoiceId).ToHashSet(StringComparer.Ordinal);
        var memoryKeys=memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Scope==MemoryScope.Team&&x.ScopeId==team.Id||x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).Select(x=>x.Key).ToHashSet(StringComparer.Ordinal);
        return package.PresentationDefinition.TeamSceneDefinitions
            .Where(x=>x.CheckpointId==checkpointId&&TargetMatches(x,entity))
            .Where(x=>EligibilityMatches(x.EligibilityDefinition,choiceIds,memoryKeys))
            .OrderByDescending(x=>x.Priority)
            .ThenByDescending(x=>x.TargetSelector?.Type==TargetSelectorType.TeamControllingEntityDefinition||x.TargetSelector is null)
            .ThenBy(x=>x.SceneId,StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static bool TargetMatches(TeamScenePresentationDefinition scene,WorldEntity entity)
    {
        var selector=scene.TargetSelector;
        if(selector is null)return scene.EntityDefinitionId==entity.DefinitionId;
        return selector.Type switch
        {
            TargetSelectorType.AllTeams=>true,
            TargetSelectorType.TeamControllingEntityDefinition=>selector.EntityDefinitionId==entity.DefinitionId,
            TargetSelectorType.EntityDefinition=>selector.EntityDefinitionId==entity.DefinitionId,
            _=>false
        };
    }

    private static bool EligibilityMatches(TeamSceneEligibilityDefinition eligibility,IReadOnlySet<string> choices,IReadOnlySet<string> memories)
        =>eligibility.RequiredChoiceIds.All(choices.Contains)
          &&eligibility.RequiredMemoryKeys.All(memories.Contains)
          &&eligibility.ForbiddenChoiceIds.All(x=>!choices.Contains(x))
          &&eligibility.ForbiddenMemoryKeys.All(x=>!memories.Contains(x));
}
