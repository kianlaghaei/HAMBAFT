using System.Text.Json;
using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;

namespace Hambaft.UnitTests;

public sealed class TeamSceneResolutionTests
{
    [Fact]
    public async Task Package_0_5_is_valid()
    {
        var services=StoryRuntimeTestSupport.PackageServices();
        var package=await services.Loader.LoadAsync("hezar-cheragh","0.5.0",default);

        package.Manifest.Version.Should().Be("0.5.0");
        services.Validator.Validate(package).IsValid.Should().BeTrue();
        package.PresentationDefinition.TeamSceneDefinitions.Should().HaveCount(3);
    }

    [Fact]
    public async Task Resolves_shared_and_business_specific_scenes_with_distinct_assets_and_hotspots()
    {
        var package=await StoryRuntimeTestSupport.PackageServices().Loader.LoadAsync("hezar-cheragh","0.5.0",default);
        var sessionId=Guid.NewGuid();
        var bakery=TeamAndEntity(sessionId,"bakery-sepideh","نانوایی سپیده");
        var logistics=TeamAndEntity(sessionId,"logistics-rah-no","باربری راه‌نو");
        var printing=TeamAndEntity(sessionId,"printing-roshan","چاپخانه روشن");
        var state=Experience(sessionId,package,[bakery.Team,logistics.Team,printing.Team],[bakery.Entity,logistics.Entity,printing.Entity]);

        var bakeryScene=ViewProjector.Team(state,bakery.Team,package).TeamScenePresentation!;
        var logisticsScene=ViewProjector.Team(state,logistics.Team,package).TeamScenePresentation!;
        var sharedScene=ViewProjector.Team(state,printing.Team,package).TeamScenePresentation!;

        bakeryScene.SceneId.Should().Be("bakery-market-entry");
        logisticsScene.SceneId.Should().Be("logistics-market-entry");
        sharedScene.SceneId.Should().Be("shared-market-entry");
        new[]{bakeryScene.BackgroundAssetId,logisticsScene.BackgroundAssetId,sharedScene.BackgroundAssetId}.Should().OnlyHaveUniqueItems();
        bakeryScene.Hotspots.Select(x=>x.StorySheetId).Should().Contain("bakery-counter").And.NotContain("dispatch-board");
        logisticsScene.Hotspots.Select(x=>x.StorySheetId).Should().Contain("dispatch-board").And.NotContain("bakery-counter");
        bakeryScene.BackgroundAssetUrl.Should().Be("/api/story/scene-asset");
    }

    [Fact]
    public async Task Team_scene_projection_does_not_expose_another_teams_scene_or_public_story_data()
    {
        var package=await StoryRuntimeTestSupport.PackageServices().Loader.LoadAsync("hezar-cheragh","0.5.0",default);
        var sessionId=Guid.NewGuid();
        var bakery=TeamAndEntity(sessionId,"bakery-sepideh","نانوایی سپیده");
        var logistics=TeamAndEntity(sessionId,"logistics-rah-no","باربری راه‌نو");
        var state=Experience(sessionId,package,[bakery.Team,logistics.Team],[bakery.Entity,logistics.Entity]);

        var bakeryJson=JsonSerializer.Serialize(ViewProjector.Team(state,bakery.Team,package));
        bakeryJson.Should().Contain("bakery-market-entry").And.NotContain("logistics-market-entry").And.NotContain("dispatch-board");

        var publicView=new PublicWorldView(sessionId,SessionStatus.Running,[bakery.Entity,logistics.Entity],[],[],[],1,package.Manifest.Id,package.Manifest.Version,package.ContentHash,"market-entry",null,null,null);
        var publicJson=JsonSerializer.Serialize(ViewProjector.Hydrate(publicView,package));
        publicJson.Should().NotContain("teamScenePresentation").And.NotContain("bakery-market-entry").And.NotContain("logistics-market-entry");
    }

    private static (Team Team,WorldEntity Entity) TeamAndEntity(Guid sessionId,string definitionId,string name)
    {
        var teamId=Guid.NewGuid();var entityId=Guid.NewGuid();
        return(new(teamId,sessionId,name,"hash",entityId,DateTimeOffset.UtcNow),new(entityId,sessionId,definitionId,name,ControllerType.HumanTeam,teamId,null,EntityStatus.Active));
    }

    private static SessionExperienceView Experience(Guid sessionId,StoryPackage package,IReadOnlyList<Team> teams,IReadOnlyList<WorldEntity> entities)
        =>new(sessionId,SessionStatus.Running,teams,entities,[],[],[],1,package.Manifest.Id,package.Manifest.Version,package.ContentHash,"market-entry",[],[]);
}
