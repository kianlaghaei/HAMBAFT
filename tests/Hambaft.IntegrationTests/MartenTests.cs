using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;
using Hambaft.Infrastructure.Persistence;
using Hambaft.Infrastructure.Security;
using JasperFx;
using Marten;
using Npgsql;

namespace Hambaft.IntegrationTests;

[Collection("postgres")]
public sealed class MartenTests(PostgresFixture fixture)
{
    [Fact]
    public void Test_database_configuration_cannot_target_sharedworld()
    {
        FluentActions.Invoking(()=>DatabaseSafety.ValidateTestConnection("Host=localhost;Database=SharedWorld;Username=test;Password=test")).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(()=>DatabaseSafety.ValidateTestConnection("Host=localhost;Database=hambaft_dev;Username=test;Password=test")).Should().Throw<InvalidOperationException>();
    }

    [PostgresFact]
    public async Task Marten_connects()
    {
        await using var connection=new NpgsqlConnection(fixture.ConnectionString);await connection.OpenAsync();connection.State.Should().Be(System.Data.ConnectionState.Open);
    }

    [PostgresFact]
    public async Task Session_stream_is_created_and_events_append_in_order()
    {
        var id=Guid.NewGuid();await Create(id);await AddTeam(id,1);
        await using var query=fixture.Store!.QuerySession();var events=await query.Events.FetchStreamAsync(id);
        events.Select(x=>x.Data.GetType().Name).Should().Equal(nameof(SessionCreated),nameof(TeamAdded));
    }

    [PostgresFact]
    public async Task Expected_version_succeeds()
    {
        var id=Guid.NewGuid();await Create(id);var result=await AddTeam(id,1);result.Result.StateVersion.Should().Be(2);
    }

    [PostgresFact]
    public async Task Stale_version_fails_without_overwrite()
    {
        var id=Guid.NewGuid();await Create(id);
        await using var session1=fixture.Store!.LightweightSession();await using var session2=fixture.Store.LightweightSession();
        var store1=new MartenSessionStore(session1);var store2=new MartenSessionStore(session2);var first=await store1.LoadAsync(id,default);var stale=await store2.LoadAsync(id,default);
        var m=Meta(id);var e1=first!.AddTeam(Guid.NewGuid(),"One","hash",m);first.Apply(e1);var e2=stale!.AddTeam(Guid.NewGuid(),"Two","hash",m);stale.Apply(e2);
        await store1.AppendAsync(id,1,[e1],first,default);
        await FluentActions.Invoking(()=>store2.AppendAsync(id,1,[e2],stale,default)).Should().ThrowAsync<ConcurrencyException>();
        await using var query=fixture.Store.QuerySession();(await query.Events.FetchStreamAsync(id)).Should().HaveCount(2);
    }

    [PostgresFact]
    public async Task Inline_projections_update()
    {
        var id=Guid.NewGuid();await Create(id);await AddTeam(id,1);
        await using var query=fixture.Store!.QuerySession();var view=await query.LoadAsync<SessionStateView>(id);view.Should().NotBeNull();view!.StateVersion.Should().Be(2);view.Teams.Should().ContainSingle();
    }

    [PostgresFact]
    public async Task Projection_replay_matches_aggregate_state()
    {
        var id=Guid.NewGuid();await Create(id);await AddTeam(id,1);
        await using var session=fixture.Store!.LightweightSession();var store=new MartenSessionStore(session);var events=await store.LoadEventsAsync(id,default);var replay=StorySession.From(events);var view=await store.LoadSessionViewAsync(id,default);
        view!.Should().BeEquivalentTo(ViewProjector.Session(replay));
    }

    [PostgresFact]
    public async Task Team_and_entity_views_can_be_rebuilt_from_events()
    {
        var id=Guid.NewGuid();await Create(id);var added=await AddTeam(id,1);var entityId=Guid.NewGuid();
        await using(var session=fixture.Store!.LightweightSession())
        {
            var runtime=new SessionRuntime(new MartenSessionStore(session),new PairingCodeGenerator());
            await runtime.ExecuteAsync(new CreateWorldEntity(id,entityId,"hero","Hero",ControllerType.HumanTeam,null,2,MetaContext()),default);
        }
        await using(var session=fixture.Store!.LightweightSession())
        {
            var runtime=new SessionRuntime(new MartenSessionStore(session),new PairingCodeGenerator());
            await runtime.ExecuteAsync(new AssignEntityToTeam(id,entityId,added.TeamId,3,MetaContext()),default);
        }

        TeamExperienceView expectedTeam;
        EntityStateView expectedEntity;
        await using(var query=fixture.Store!.QuerySession())
        {
            var experience=(await query.LoadAsync<SessionExperienceView>(id))!;
            expectedTeam=ViewProjector.Team(experience,experience.Teams.Single(x=>x.Id==added.TeamId));
            expectedEntity=ViewProjector.Entity(experience,experience.Entities.Single(x=>x.Id==entityId));
        }

        using(var daemon=await fixture.Store!.BuildProjectionDaemonAsync())
            await daemon.RebuildProjectionAsync("SessionExperience",default);

        await using var rebuilt=fixture.Store.QuerySession();
        var rebuiltExperience=(await rebuilt.LoadAsync<SessionExperienceView>(id))!;
        ViewProjector.Team(rebuiltExperience,rebuiltExperience.Teams.Single(x=>x.Id==added.TeamId)).Should().BeEquivalentTo(expectedTeam);
        ViewProjector.Entity(rebuiltExperience,rebuiltExperience.Entities.Single(x=>x.Id==entityId)).Should().BeEquivalentTo(expectedEntity);
    }

    [PostgresFact]
    public async Task Public_projection_excludes_private_memories()
    {
        var id=Guid.NewGuid();await Create(id);var teamResult=await AddTeam(id,1);var teamId=teamResult.TeamId;
        await using(var session=fixture.Store!.LightweightSession()){var runtime=new SessionRuntime(new MartenSessionStore(session),new PairingCodeGenerator());await runtime.ExecuteAsync(new AddInitialMemory(id,MemoryScope.Team,teamId,"secret","{\"value\":1}",MemoryVisibility.TeamPrivate,2,MetaContext()),default);}
        await using var query=fixture.Store.QuerySession();var publicView=await query.LoadAsync<PublicWorldView>(id);var experience=await query.LoadAsync<SessionExperienceView>(id);var teamView=ViewProjector.Team(experience!,experience!.Teams.Single(x=>x.Id==teamId));
        publicView!.PublicMemories.Should().BeEmpty();teamView!.VisibleMemories.Should().ContainSingle(x=>x.Key=="secret");
    }

    private async Task Create(Guid id)
    {
        await using var session=fixture.Store!.LightweightSession();var runtime=new SessionRuntime(new MartenSessionStore(session),new PairingCodeGenerator());await runtime.ExecuteAsync(new CreateSession(id,"pkg","1","hash",42,0,MetaContext()),default);
    }
    private async Task<(CommandResult Result,Guid TeamId)> AddTeam(Guid id,long version)
    {
        var team=Guid.NewGuid();await using var session=fixture.Store!.LightweightSession();var runtime=new SessionRuntime(new MartenSessionStore(session),new PairingCodeGenerator());var result=await runtime.ExecuteAsync(new AddTeam(id,team,"Team",version,MetaContext()),default);return(result,team);
    }
    private static EventMetadata Meta(Guid id)=>MetaContext().For(id);
    private static CommandContext MetaContext(){var id=Guid.NewGuid();return new(id,id,id,null,DateTimeOffset.UtcNow);}
}
