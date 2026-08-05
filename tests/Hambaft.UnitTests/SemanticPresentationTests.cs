using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;

namespace Hambaft.UnitTests;

public sealed class SemanticPresentationTests
{
    [Theory]
    [InlineData(24,"calm")]
    [InlineData(25,"uneasy")]
    [InlineData(49,"uneasy")]
    [InlineData(50,"strained")]
    [InlineData(74,"strained")]
    [InlineData(75,"critical")]
    public async Task Pressure_boundaries_map_to_package_authored_bands(decimal value,string bandId)
    {
        var package=await Package();var metric=new Metric(MetricScope.World,Guid.NewGuid(),"Pressure",value);
        SemanticPresentation.Map(metric,package.PresentationDefinition.MetricBands.Single(x=>x.MetricKey=="Pressure").Bands)!.BandId.Should().Be(bandId);
    }

    [Fact]
    public async Task Team_and_public_views_remove_raw_metrics_when_semantic_presentation_exists()
    {
        var package=await Package();var sessionId=Guid.NewGuid();var teamId=Guid.NewGuid();var entityId=Guid.NewGuid();
        var team=new Team(teamId,sessionId,"سپیده","hash",entityId,DateTimeOffset.UtcNow);
        var entity=new WorldEntity(entityId,sessionId,"bakery-sepideh","نانوایی سپیده",ControllerType.HumanTeam,teamId,null,EntityStatus.Active);
        var metrics=new List<Metric>{new(MetricScope.World,sessionId,"Pressure",67),new(MetricScope.Entity,entityId,"Inventory",34),new(MetricScope.Entity,entityId,"LocalTrust",42)};
        var experience=new SessionExperienceView(sessionId,SessionStatus.Running,[team],[entity],metrics,[],[],12,package.Manifest.Id,package.Manifest.Version,package.ContentHash,"cargo-did-not-arrive",[],[]);
        var teamView=ViewProjector.Team(experience,team,package);
        teamView.VisibleMetrics.Should().BeEmpty();teamView.BusinessPresentation!.Pulse.Should().NotBeEmpty();teamView.WorldPresentation!.Pulse.Should().Contain("بازار زیر فشار است");
        var publicView=new PublicWorldView(sessionId,SessionStatus.Running,[entity],metrics.Where(x=>x.Scope==MetricScope.World).ToList(),[],[],12,package.Manifest.Id,package.Manifest.Version,package.ContentHash,"cargo-did-not-arrive",null,null,null);
        var hydrated=ViewProjector.Hydrate(publicView,package);
        hydrated.WorldMetrics.Should().BeEmpty();hydrated.WorldPresentation!.Pulse.Should().Contain("بازار زیر فشار است");hydrated.PublicRelationships.Should().BeEmpty();
    }

    [Fact]
    public async Task Package_0_3_is_independent_and_presentation_files_are_hash_locked()
    {
        var services=StoryRuntimeTestSupport.PackageServices();var oldPackage=await services.Loader.LoadAsync("hezar-cheragh","0.2.0",default);var current=await services.Loader.LoadAsync("hezar-cheragh","0.3.0",default);
        current.Manifest.Version.Should().Be("0.3.0");current.ContentHash.Should().NotBe(oldPackage.ContentHash);current.PresentationDefinition.Locations.Should().HaveCount(13);current.PresentationDefinition.MetricBands.Should().HaveCount(5);
    }

    [Fact]
    public void Public_projection_does_not_accept_relationship_events()
    {
        var session=Guid.NewGuid();var first=Guid.NewGuid();var second=Guid.NewGuid();var metadata=new EventMetadata(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),session,null,DateTimeOffset.UtcNow);var projection=new Hambaft.Infrastructure.Persistence.PublicWorldProjection();
        var view=projection.Create(new SessionCreated(session,"hezar-cheragh","0.3.0","hash",42,metadata));
        view=projection.Apply(new InitialRelationshipSet(first,second,"Trust",80,metadata),view);
        view.PublicRelationships.Should().BeEmpty();
    }

    private static Task<StoryPackage> Package()=>StoryRuntimeTestSupport.PackageServices().Loader.LoadAsync("hezar-cheragh","0.3.0",default);
}
