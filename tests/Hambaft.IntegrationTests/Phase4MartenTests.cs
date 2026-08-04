using System.Diagnostics;
using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;
using Hambaft.Infrastructure.Persistence;
using Hambaft.Infrastructure.StoryPackages;
using Hambaft.Narrative.Ink;
using JasperFx;

namespace Hambaft.IntegrationTests;

[Collection("postgres")]
public sealed class Phase4MartenTests(PostgresFixture fixture)
{
    [PostgresFact]
    public async Task Dual_endings_persist_atomically_rebuild_and_reject_concurrent_resolution()
    {
        var loader=Packages();var package=await loader.LoadAsync("hezar-cheragh","0.1.0",default);var sessionId=Guid.NewGuid();var setup=Setup(package,sessionId);var setupState=StorySession.From(setup);
        await using(var document=fixture.Store!.LightweightSession())await new MartenSessionStore(document,loader).AppendAsync(sessionId,0,setup,setupState,default);

        await using(var narrativeQuery=fixture.Store.LightweightSession())
        {
            var inkLoader=new FileInkStoryLoader(new(FindStories()));var narrativeStore=new MartenSessionStore(narrativeQuery,loader,new InkNarrativeRenderer(inkLoader,new InkStateSerializer()));
            var publicView=await narrativeStore.LoadPublicViewAsync(sessionId,default);var firstTeam=setupState.Teams[0];var secondTeam=setupState.Teams[1];var bakery=await narrativeStore.LoadTeamViewAsync(firstTeam.Id,default);var logistics=await narrativeStore.LoadTeamViewAsync(secondTeam.Id,default);
            publicView!.Narrative!.Paragraphs.Should().Contain(x=>x.Contains("زنگ بازار"));bakery!.PrivateStorylets.Single().Paragraphs.Should().Contain(x=>x.Contains("رحیم"));logistics!.PrivateStorylets.Single().Paragraphs.Should().NotContain(x=>x.Contains("رحیم"));
            System.Text.Json.JsonSerializer.Serialize(publicView).Should().NotContain("تکه دفتر شما");
        }

        await using var documentA=fixture.Store!.LightweightSession();await using var documentB=fixture.Store.LightweightSession();var storeA=new MartenSessionStore(documentA,loader);var storeB=new MartenSessionStore(documentB,loader);
        var stateA=(await storeA.LoadAsync(sessionId,default))!;var stateB=(await storeB.LoadAsync(sessionId,default))!;var historyA=await storeA.LoadEventsAsync(sessionId,default);var historyB=await storeB.LoadEventsAsync(sessionId,default);
        var batchA=BuildEndings(package,stateA,historyA);var batchB=BuildEndings(package,stateB,historyB);
        batchA.Select(x=>x.GetType()).Should().Equal(batchB.Select(x=>x.GetType()));
        await storeA.AppendAsync(sessionId,setup.Count,batchA,stateA,default);
        await FluentActions.Awaiting(()=>storeB.AppendAsync(sessionId,setup.Count,batchB,stateB,default)).Should().ThrowAsync<Exception>();

        await using(var query=fixture.Store.LightweightSession())
        {
            var store=new MartenSessionStore(query,loader);var events=await store.LoadEventsAsync(sessionId,default);var aggregate=StorySession.From(events);
            aggregate.Status.Should().Be(SessionStatus.Completed);aggregate.EndingResults.Should().HaveCount(3);events.OfType<EntityEndingResolved>().Should().HaveCount(2);events.OfType<WorldEndingResolved>().Should().ContainSingle();events.OfType<SessionCompleted>().Should().ContainSingle();
            aggregate.EndingResults.Should().OnlyContain(x=>x.PackageId=="hezar-cheragh"&&x.PackageVersion=="0.1.0"&&x.ContentHash==package.ContentHash&&x.NarrativeRef.StartsWith("ending-"));
            var evidence=await store.LoadEndingEvidenceAsync(sessionId,default);evidence!.EntityEndings.Should().HaveCount(2);evidence.WorldEnding.Should().NotBeNull();
            var publicView=await store.LoadPublicViewAsync(sessionId,default);publicView!.Status.Should().Be(SessionStatus.Completed);publicView.CurrentWorldNarrativeRef.Should().Be("a-public");
            EventFingerprint.Compute(events).Should().NotBeNullOrWhiteSpace();
        }

        var rebuildTimer=Stopwatch.StartNew();
        using(var daemon=await fixture.Store.BuildProjectionDaemonAsync())
        {
            await daemon.RebuildProjectionAsync("EndingEvidence",default);await daemon.RebuildProjectionAsync("SessionExperience",default);await daemon.RebuildProjectionAsync("SessionState",default);await daemon.RebuildProjectionAsync("PublicWorld",default);
        }
        rebuildTimer.Stop();rebuildTimer.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(20),"a local representative Phase 4 projection rebuild should remain an inexpensive regression check");
        await using(var rebuilt=fixture.Store.QuerySession())
        {
            var evidence=await rebuilt.LoadAsync<EndingEvidenceView>(sessionId);var state=await rebuilt.LoadAsync<SessionStateView>(sessionId);
            evidence!.EntityEndings.Should().HaveCount(2);evidence.WorldEnding.Should().NotBeNull();state!.Status.Should().Be(SessionStatus.Completed);state.EndingResults.Should().HaveCount(3);
        }
    }

    private static IReadOnlyList<IDomainEvent> BuildEndings(StoryPackage package,StorySession state,IReadOnlyList<IDomainEvent> history)
    {
        var resolver=new DeterministicEndingResolver(new ConditionEngine());var result=new List<IDomainEvent>();var metadata=Meta(state.Id);
        foreach(var entity in state.Entities.Where(x=>x.ControllerType==ControllerType.HumanTeam).OrderBy(x=>x.DefinitionId,StringComparer.Ordinal))
        {
            var ending=((IEntityEndingResolver)resolver).Resolve(package,state,entity,history,state.StateVersion+1,metadata.OccurredAtUtc);var e=state.ResolveEntityEnding(ending,metadata);state.Apply(e);result.Add(e);
        }
        var world=((IWorldEndingResolver)resolver).Resolve(package,state,history,state.StateVersion+1,metadata.OccurredAtUtc);var worldEvent=state.ResolveWorldEnding(world,metadata);state.Apply(worldEvent);result.Add(worldEvent);var completed=state.Complete(metadata);state.Apply(completed);result.Add(completed);return result;
    }

    private static IReadOnlyList<IDomainEvent> Setup(StoryPackage package,Guid sessionId)
    {
        var events=new List<IDomainEvent>();var created=StorySession.Create(sessionId,package.Manifest.Id,package.Manifest.Version,package.ContentHash,42,Meta(sessionId));events.Add(created);var state=StorySession.From(events);
        for(var i=0;i<2;i++)
        {
            var team=DeterministicIds.Create(sessionId,"team",i);var entity=DeterministicIds.Create(sessionId,"entity",i);Add(state,events,state.AddTeam(team,$"Team {i}","hash",Meta(sessionId)));Add(state,events,state.CreateWorldEntity(entity,package.Entities[i].Id,package.Entities[i].DisplayName,ControllerType.HumanTeam,null,Meta(sessionId)));Add(state,events,state.AssignEntityToTeam(entity,team,Meta(sessionId)));
        }
        Add(state,events,state.Start(Meta(sessionId)));Add(state,events,state.InitializeNarrative(package.Manifest.Id,package.Manifest.Version,package.ContentHash,"morning-without-bell",Meta(sessionId)));
        for(var i=0;i<state.Teams.Count;i++)
        {
            var storylet=package.Storylets.Single(x=>x.Id==(i==0?"hc-a-bakery":"hc-a-logistics"));var entity=state.Entities[i];var assignment=DeterministicIds.Create(sessionId,"assignment",i);Add(state,events,state.AssignStorylet(assignment,storylet.Id,storylet.CheckpointId,storylet.Scope,state.Teams[i].Id,entity.Id,true,state.StateVersion+1,Meta(sessionId)));
        }
        Add(state,events,new WorldNarrativePublished("hc-a-public","a-public","morning-without-bell",1,"$runtime",Meta(sessionId)));
        foreach(var metric in package.Metrics.Where(x=>x.Scope==MetricScope.World))Add(state,events,new MetricSet(MetricScope.World,sessionId,metric.Key,metric.DefaultValue,metric.DefaultValue,"$initialize",Meta(sessionId)));
        foreach(var entity in state.Entities)foreach(var metric in package.Metrics.Where(x=>x.Scope==MetricScope.Entity)){var value=package.Entities.Single(x=>x.Id==entity.DefinitionId).InitialMetrics[metric.Key];Add(state,events,new MetricSet(MetricScope.Entity,entity.Id,metric.Key,metric.DefaultValue,value,"$initialize",Meta(sessionId)));}
        return events;
    }
    private static void Add(StorySession state,List<IDomainEvent> events,IDomainEvent e){state.Apply(e);events.Add(e);}
    private static EventMetadata Meta(Guid sessionId){var command=DeterministicIds.Create(sessionId,"phase4-integration",Guid.NewGuid());return new(command,command,command,sessionId,null,new DateTimeOffset(2026,8,4,12,0,0,TimeSpan.Zero),"slice-complete");}
    private static IStoryPackageLoader Packages(){var root=FindStories();return new FileSystemStoryPackageLoader(new StoryPackageOptions{Root=root},new DeterministicStoryPackageHasher(),new StoryPackageValidator());}
    private static string FindStories(){var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!Directory.Exists(Path.Combine(directory.FullName,"stories")))directory=directory.Parent;return Path.Combine(directory!.FullName,"stories");}
}
