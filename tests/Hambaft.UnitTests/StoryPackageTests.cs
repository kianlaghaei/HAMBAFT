using System.Text.Json;
using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;

namespace Hambaft.UnitTests;

public sealed class StoryPackageTests
{
    [Fact] public async Task Valid_package_loads(){var s=StoryRuntimeTestSupport.PackageServices();var p=await s.Loader.LoadAsync("sample-cargo-delay","1.0.0",default);p.Manifest.Id.Should().Be("sample-cargo-delay");p.ContentHash.Should().StartWith("sha256:");}
    [Fact] public async Task Versions_1_0_and_1_1_load_independently_with_distinct_hashes()
    {
        var services=StoryRuntimeTestSupport.PackageServices();var oldPackage=await services.Loader.LoadAsync("sample-cargo-delay","1.0.0",default);var phase3=await services.Loader.LoadAsync("sample-cargo-delay","1.1.0",default);
        oldPackage.Manifest.Version.Should().Be("1.0.0");oldPackage.InteractionDefinitions.Should().BeEmpty();phase3.Manifest.Version.Should().Be("1.1.0");phase3.InteractionDefinitions.Should().ContainSingle(x=>x.Id=="emergency-capacity-support");phase3.ContentHash.Should().NotBe(oldPackage.ContentHash);
    }
    [Fact] public async Task Session_locked_to_1_0_cannot_hydrate_from_1_1()
    {
        var scenario=await StoryRuntimeTestSupport.CreateStartedAsync();var phase3=await StoryRuntimeTestSupport.PackageServices().Loader.LoadAsync("sample-cargo-delay","1.1.0",default);var runtime=new Hambaft.Application.SessionRuntime(scenario.Store,new FixedTestPairing(),new FixedPackageLoader(phase3),new Hambaft.Application.DeterministicStoryletSelector(new Hambaft.Application.ConditionEngine()),new Hambaft.Application.EffectEngine(),new Hambaft.Application.DeterministicBehaviorResolver(new Hambaft.Application.ConditionEngine()));await FluentActions.Invoking(()=>runtime.ExecuteAsync(new Hambaft.Application.InitializeNarrative(scenario.SessionId,8,StoryRuntimeTestSupport.Context()),default)).Should().ThrowAsync<Hambaft.Application.StoryPackageHashMismatchException>();
    }
    [Fact] public async Task Duplicate_ids_fail(){var(s,p)=await Package();var duplicate=p with { Metrics=p.Metrics.Append(p.Metrics[0]).ToList() };s.Validate(duplicate).Errors.Should().Contain(x=>x.ErrorCode=="duplicate-metric-id");}
    [Fact] public async Task Missing_narrative_reference_fails(){var(s,p)=await Package();var changed=p with { Storylets=p.Storylets.Select((x,i)=>i==0?x with { NarrativeRef="missing" }:x).ToList() };s.Validate(changed).Errors.Should().Contain(x=>x.ErrorCode=="missing-narrative-reference");}
    [Fact] public async Task Unknown_effect_fails(){var(s,p)=await Package();var story=p.Storylets[1];var choice=story.Choices[0] with { EffectIds=["missing"] };var changed=p with { Storylets=p.Storylets.Select(x=>x.Id==story.Id?x with { Choices=[choice,..story.Choices.Skip(1)] }:x).ToList() };s.Validate(changed).Errors.Should().Contain(x=>x.ErrorCode=="unknown-effect");}
    [Fact] public async Task Unknown_metric_fails(){var(s,p)=await Package();var effect=p.Effects[0] with { MetricKey="missing" };var changed=p with { Effects=p.Effects.Select((x,i)=>i==0?effect:x).ToList() };s.Validate(changed).Errors.Should().Contain(x=>x.ErrorCode=="unknown-metric");}
    [Fact] public async Task Invalid_choice_reference_fails(){var(s,p)=await Package();var story=p.Storylets[1];var choice=story.Choices[0] with { LabelRef="missing" };var changed=p with { Storylets=p.Storylets.Select(x=>x.Id==story.Id?x with { Choices=[choice,..story.Choices.Skip(1)] }:x).ToList() };s.Validate(changed).Errors.Should().Contain(x=>x.ErrorCode=="invalid-choice-reference");}
    [Fact] public async Task Invalid_checkpoint_fails(){var(s,p)=await Package();var changed=p with { Manifest=p.Manifest with { EntryCheckpointId="missing" } };s.Validate(changed).Errors.Should().Contain(x=>x.ErrorCode=="missing-entry-checkpoint");}
    [Fact] public async Task Canonical_hash_ignores_json_formatting()
    {
        var services=StoryRuntimeTestSupport.PackageServices();var source=Path.Combine(StoryRuntimeTestSupport.StoriesRoot,"sample-cargo-delay","1.0.0");var temp=Path.Combine(Path.GetTempPath(),"hambaft-hash-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        try { foreach(var file in Directory.GetFiles(source)){var target=Path.Combine(temp,Path.GetFileName(file));using var json=JsonDocument.Parse(await File.ReadAllTextAsync(file));if(Path.GetFileName(file)=="manifest.json"){var reversed=json.RootElement.EnumerateObject().Reverse().ToDictionary(x=>x.Name,x=>x.Value.Clone());await File.WriteAllTextAsync(target,JsonSerializer.Serialize(reversed,new JsonSerializerOptions{WriteIndented=true}));}else await File.WriteAllTextAsync(target,JsonSerializer.Serialize(json.RootElement,new JsonSerializerOptions{WriteIndented=true}));}var a=await services.Hasher.ComputeAsync(source,default);var b=await services.Hasher.ComputeAsync(temp,default);b.Should().Be(a); }
        finally { Directory.Delete(temp,true); }
    }
    [Fact] public async Task Semantic_change_changes_hash()
    {
        var services=StoryRuntimeTestSupport.PackageServices();var source=Path.Combine(StoryRuntimeTestSupport.StoriesRoot,"sample-cargo-delay","1.0.0");var temp=Path.Combine(Path.GetTempPath(),"hambaft-hash-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        try { foreach(var file in Directory.GetFiles(source))File.Copy(file,Path.Combine(temp,Path.GetFileName(file)));var path=Path.Combine(temp,"manifest.json");await File.WriteAllTextAsync(path,(await File.ReadAllTextAsync(path)).Replace("10,","11,"));(await services.Hasher.ComputeAsync(temp,default)).Should().NotBe(await services.Hasher.ComputeAsync(source,default)); }
        finally { Directory.Delete(temp,true); }
    }

    [Fact]
    public async Task Package_0_4_team_scene_presentation_is_valid_and_team_specific()
    {
        var services=StoryRuntimeTestSupport.PackageServices();var package=await services.Loader.LoadAsync("hezar-cheragh","0.4.0",default);
        package.Manifest.Version.Should().Be("0.4.0");package.PresentationDefinition.TeamSceneDefinitions.Should().HaveCount(4);package.PresentationDefinition.TeamSceneDefinitions.Should().OnlyContain(x=>x.RequiredInvestigationCount==2);
        var sessionId=Guid.NewGuid();var bakeryTeamId=Guid.NewGuid();var logisticsTeamId=Guid.NewGuid();var bakeryEntityId=Guid.NewGuid();var logisticsEntityId=Guid.NewGuid();
        var bakeryTeam=new Team(bakeryTeamId,sessionId,"سپیده","hash",bakeryEntityId,DateTimeOffset.UtcNow);var logisticsTeam=new Team(logisticsTeamId,sessionId,"راه‌نو","hash",logisticsEntityId,DateTimeOffset.UtcNow);
        var bakeryEntity=new WorldEntity(bakeryEntityId,sessionId,"bakery-sepideh","نانوایی سپیده",ControllerType.HumanTeam,bakeryTeamId,null,EntityStatus.Active);var logisticsEntity=new WorldEntity(logisticsEntityId,sessionId,"logistics-rah-no","باربری راه‌نو",ControllerType.HumanTeam,logisticsTeamId,null,EntityStatus.Active);
        var bakeryAssignmentId=Guid.NewGuid();var logisticsAssignmentId=Guid.NewGuid();var assignments=new[]
        {
            new StoryletAssignment(bakeryAssignmentId,"hc-a-bakery","morning-without-bell",StoryletScope.TeamPrivate,bakeryTeamId,bakeryEntityId,true,1,StoryletAssignmentStatus.Responded),
            new StoryletAssignment(logisticsAssignmentId,"hc-a-logistics","morning-without-bell",StoryletScope.TeamPrivate,logisticsTeamId,logisticsEntityId,true,1,StoryletAssignmentStatus.Assigned)
        };
        var submissions=new[] { new SubmittedStoryChoice(bakeryAssignmentId,bakeryTeamId,"bakery-hold-fragment",DateTimeOffset.UtcNow,2,Guid.NewGuid()) };
        var experience=new SessionExperienceView(sessionId,SessionStatus.Running,[bakeryTeam,logisticsTeam],[bakeryEntity,logisticsEntity],[],[],[],5,package.Manifest.Id,package.Manifest.Version,package.ContentHash,"morning-without-bell",assignments,submissions);
        var bakeryView=ViewProjector.Team(experience,bakeryTeam,package);var logisticsView=ViewProjector.Team(experience,logisticsTeam,package);
        bakeryView.TeamScenePresentation!.BusinessActivity!.Summary.Should().Contain("سهم آرد");bakeryView.TeamScenePresentation.FinalDecisionPresentation!.SelectedChoice!.ChoiceId.Should().Be("bakery-hold-fragment");bakeryView.TeamScenePresentation.MarketReactions.Should().HaveCount(2);
        logisticsView.TeamScenePresentation!.BusinessActivity!.Title.Should().Contain("مسیر");logisticsView.TeamScenePresentation.FinalDecisionPresentation!.SelectedChoiceId.Should().BeNull();logisticsView.TeamScenePresentation.InvestigationLocations.Select(x=>x.Id).Should().Contain("logistics-rah-no").And.NotContain("haj-sadegh-office");
    }

    [Fact]
    public async Task Team_scene_presentation_is_not_part_of_public_view()
    {
        var services=StoryRuntimeTestSupport.PackageServices();var package=await services.Loader.LoadAsync("hezar-cheragh","0.4.0",default);var sessionId=Guid.NewGuid();var entityId=Guid.NewGuid();
        var publicView=new PublicWorldView(sessionId,SessionStatus.Running,[new WorldEntity(entityId,sessionId,"bakery-sepideh","نانوایی سپیده",ControllerType.HumanTeam,Guid.NewGuid(),null,EntityStatus.Active)],[],[],[],1,package.Manifest.Id,package.Manifest.Version,package.ContentHash,"morning-without-bell",null,null,null);
        var hydrated=ViewProjector.Hydrate(publicView,package);JsonSerializer.Serialize(hydrated).Should().NotContain("teamScenePresentation").And.NotContain("hidden-stall-bakery");
    }

    private static async Task<(Hambaft.Application.IStoryPackageValidator,StoryPackage)> Package(){var s=StoryRuntimeTestSupport.PackageServices();return(s.Validator,await s.Loader.LoadAsync("sample-cargo-delay","1.0.0",default));}
}
