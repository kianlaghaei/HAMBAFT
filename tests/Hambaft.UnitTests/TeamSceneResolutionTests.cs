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

    [Fact]
    public async Task Package_0_6_contains_shared_intro_four_business_scenes_and_shared_reaction()
    {
        var package=await StoryRuntimeTestSupport.PackageServices().Loader.LoadAsync("hezar-cheragh","0.6.0",default);
        var sessionId=Guid.NewGuid();
        var bakery=TeamAndEntity(sessionId,"bakery-sepideh","نانوایی سپیده");
        var logistics=TeamAndEntity(sessionId,"logistics-rah-no","باربری راه‌نو");
        var printing=TeamAndEntity(sessionId,"printing-roshan","چاپخانه روشن");
        var exchange=TeamAndEntity(sessionId,"exchange-mizan","صرافی میزان");
        var teams=new[]{bakery.Team,logistics.Team,printing.Team,exchange.Team};
        var entities=new[]{bakery.Entity,logistics.Entity,printing.Entity,exchange.Entity};

        package.PresentationDefinition.TeamSceneDefinitions.Should().HaveCount(6);
        package.PresentationDefinition.TeamSceneDefinitions.Single(x=>x.CheckpointId=="act-one-intro").TargetSelector!.Type.Should().Be(TargetSelectorType.AllTeams);
        package.PresentationDefinition.TeamSceneDefinitions.Single(x=>x.CheckpointId=="act-one-reaction").TargetSelector!.Type.Should().Be(TargetSelectorType.AllTeams);
        package.PresentationDefinition.TeamSceneDefinitions.Where(x=>x.CheckpointId=="act-one-business").Should().OnlyContain(x=>x.MarketReactions.Count==2);
        var businessState=Experience(sessionId,package,teams,entities,"act-one-business");
        var views=teams.Select(team=>ViewProjector.Team(businessState,team,package).TeamScenePresentation!).ToList();
        views.Select(x=>x.SceneId).Should().OnlyHaveUniqueItems().And.HaveCount(4);
        views.Select(x=>x.BackgroundAssetId).Should().OnlyHaveUniqueItems();
        views.Should().OnlyContain(x=>x.Hotspots.Count==2&&x.StorySheets.Count==2&&x.StorySheets.All(sheet=>sheet.Narrative.Count>=3&&sheet.Narrative.Count<=5&&sheet.Evidence.Count>0)&&x.BusinessActivity!=null&&x.FinalDecisionPresentation!=null);
    }

    [Fact]
    public async Task Package_0_6_business_scene_is_private_across_all_four_teams()
    {
        var package=await StoryRuntimeTestSupport.PackageServices().Loader.LoadAsync("hezar-cheragh","0.6.0",default);
        var sessionId=Guid.NewGuid();
        var bakery=TeamAndEntity(sessionId,"bakery-sepideh","نانوایی سپیده");
        var logistics=TeamAndEntity(sessionId,"logistics-rah-no","باربری راه‌نو");
        var printing=TeamAndEntity(sessionId,"printing-roshan","چاپخانه روشن");
        var exchange=TeamAndEntity(sessionId,"exchange-mizan","صرافی میزان");
        var state=Experience(sessionId,package,[bakery.Team,logistics.Team,printing.Team,exchange.Team],[bakery.Entity,logistics.Entity,printing.Entity,exchange.Entity],"act-one-business");
        var bakeryJson=JsonSerializer.Serialize(ViewProjector.Team(state,bakery.Team,package));
        bakeryJson.Should().Contain("act-one-bakery").And.NotContain("act-one-logistics").And.NotContain("act-one-printing").And.NotContain("act-one-exchange").And.NotContain("exchange-ledger");
    }

    private static (Team Team,WorldEntity Entity) TeamAndEntity(Guid sessionId,string definitionId,string name)
    {
        var teamId=Guid.NewGuid();var entityId=Guid.NewGuid();
        return(new(teamId,sessionId,name,"hash",entityId,DateTimeOffset.UtcNow),new(entityId,sessionId,definitionId,name,ControllerType.HumanTeam,teamId,null,EntityStatus.Active));
    }

    private static SessionExperienceView Experience(Guid sessionId,StoryPackage package,IReadOnlyList<Team> teams,IReadOnlyList<WorldEntity> entities,string checkpoint="market-entry")
        =>new(sessionId,SessionStatus.Running,teams,entities,[],[],[],1,package.Manifest.Id,package.Manifest.Version,package.ContentHash,checkpoint,[],[]);
}
