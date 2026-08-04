using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;
using Hambaft.Infrastructure.Persistence;
using Hambaft.Infrastructure.Security;
using Hambaft.Infrastructure.StoryPackages;

namespace Hambaft.IntegrationTests;

[Collection("postgres")]
public sealed class NarrativeMartenTests(PostgresFixture fixture)
{
    [PostgresFact]
    public async Task Projection_hydration_detects_locked_package_mismatch()
    {
        var packageServices=Packages();var id=Guid.NewGuid();var metadata=C().For(id);
        await using(var session=fixture.Store!.LightweightSession()){session.Events.StartStream<StorySession>(id,new SessionCreated(id,"sample-cargo-delay","1.0.0","sha256:mismatch",42,metadata),new NarrativeInitialized("sample-cargo-delay","1.0.0","sha256:mismatch","response",metadata with { CheckpointId="response" }),new WorldNarrativePublished("cargo-delay-public-entry","cargo-delay-opening","response",1,"$runtime",metadata with { CheckpointId="response" }));await session.SaveChangesAsync();}
        await using var query=fixture.Store!.LightweightSession();var store=new MartenSessionStore(query,packageServices.Loader);await FluentActions.Invoking(()=>store.LoadPublicViewAsync(id,default)).Should().ThrowAsync<StoryPackageHashMismatchException>();
    }

    [PostgresFact]
    public async Task Complete_narrative_is_atomic_private_persisted_and_rebuildable()
    {
        var packageServices=Packages();var package=await packageServices.Loader.LoadAsync("sample-cargo-delay","1.0.0",default);
        var sessionId=Guid.NewGuid();var supplierTeam=Guid.NewGuid();var carrierTeam=Guid.NewGuid();var supplier=Guid.NewGuid();var carrier=Guid.NewGuid();
        long initializedVersion;long resolvedVersion;string fingerprint;
        await using(var document=fixture.Store!.LightweightSession())
        {
            var runtime=Runtime(new MartenSessionStore(document,packageServices.Loader),packageServices.Loader);
            await runtime.ExecuteAsync(new CreateSession(sessionId,package.Manifest.Id,package.Manifest.Version,package.ContentHash,42,0,C()),default);
            await runtime.ExecuteAsync(new AddTeam(sessionId,supplierTeam,"Supplier",1,C()),default);await runtime.ExecuteAsync(new AddTeam(sessionId,carrierTeam,"Carrier",2,C()),default);
            await runtime.ExecuteAsync(new CreateWorldEntity(sessionId,supplier,"supplier","Supplier",ControllerType.HumanTeam,null,3,C()),default);await runtime.ExecuteAsync(new CreateWorldEntity(sessionId,carrier,"carrier","Carrier",ControllerType.HumanTeam,null,4,C()),default);
            await runtime.ExecuteAsync(new AssignEntityToTeam(sessionId,supplier,supplierTeam,5,C()),default);await runtime.ExecuteAsync(new AssignEntityToTeam(sessionId,carrier,carrierTeam,6,C()),default);await runtime.ExecuteAsync(new StartSession(sessionId,7,C()),default);
            var initialized=await runtime.ExecuteAsync(new InitializeNarrative(sessionId,8,C()),default);initializedVersion=initialized.StateVersion;
            var state=(await new MartenSessionStore(document,packageServices.Loader).LoadAsync(sessionId,default))!;var supplierAssignment=state.StoryletAssignments.Single(x=>x.StoryletId=="supplier-delay-response");var carrierAssignment=state.StoryletAssignments.Single(x=>x.StoryletId=="carrier-delay-response");
            var first=await runtime.ExecuteAsync(new SubmitStoryChoice(sessionId,supplierAssignment.AssignmentId,"disclose-full-delay",initialized.StateVersion,C(supplierTeam)),default);var second=await runtime.ExecuteAsync(new SubmitStoryChoice(sessionId,carrierAssignment.AssignmentId,"reserve-emergency-capacity",first.StateVersion,C(carrierTeam)),default);var resolved=await runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(sessionId,second.StateVersion,C()),default);resolvedVersion=resolved.StateVersion;
            var events=await new MartenSessionStore(document,packageServices.Loader).LoadEventsAsync(sessionId,default);fingerprint=EventFingerprint.Compute(events);
        }
        await using(var query=fixture.Store!.LightweightSession())
        {
            var store=new MartenSessionStore(query,packageServices.Loader);var events=await store.LoadEventsAsync(sessionId,default);events.Should().Contain(x=>x is StoryletAssigned).And.Contain(x=>x is StoryChoiceSubmitted).And.Contain(x=>x is MetricChanged).And.Contain(x=>x is NarrativeCheckpointResolved);
            var aggregate=StorySession.From(events);aggregate.StateVersion.Should().Be(resolvedVersion);aggregate.CurrentCheckpointId.Should().Be("outcome");aggregate.CurrentWorldStoryletId.Should().Be("cargo-delay-coordinated-outcome");EventFingerprint.Compute(events).Should().Be(fingerprint);
            var supplierView=await store.LoadTeamViewAsync(supplierTeam,default);var carrierView=await store.LoadTeamViewAsync(carrierTeam,default);supplierView!.PrivateStorylets.Should().NotContain(x=>x.StoryletId=="carrier-delay-response");carrierView!.PrivateStorylets.Should().NotContain(x=>x.StoryletId=="supplier-delay-response");
            var publicView=await store.LoadPublicViewAsync(sessionId,default);publicView!.Narrative!.StoryletId.Should().Be("cargo-delay-coordinated-outcome");System.Text.Json.JsonSerializer.Serialize(publicView).Should().NotContain("supplier-delay-response");
            await FluentActions.Invoking(()=>Runtime(store,packageServices.Loader).ExecuteAsync(new ResolveNarrativeCheckpoint(sessionId,initializedVersion,C()),default)).Should().ThrowAsync<ConcurrencyConflictException>();
        }
        using(var daemon=await fixture.Store!.BuildProjectionDaemonAsync()){await daemon.RebuildProjectionAsync("SessionExperience",default);await daemon.RebuildProjectionAsync("SessionState",default);await daemon.RebuildProjectionAsync("PublicWorld",default);}
        await using(var rebuilt=fixture.Store.QuerySession()){var state=await rebuilt.LoadAsync<SessionStateView>(sessionId);state!.StateVersion.Should().Be(resolvedVersion);state.CurrentCheckpointId.Should().Be("outcome");}
    }

    private static SessionRuntime Runtime(ISessionStore store,IStoryPackageLoader loader)=>new(store,new PairingCodeGenerator(),loader,new DeterministicStoryletSelector(new ConditionEngine()),new EffectEngine());
    private static (IStoryPackageLoader Loader,IStoryPackageHasher Hasher) Packages(){var root=FindStories();var hasher=new DeterministicStoryPackageHasher();var validator=new StoryPackageValidator();return(new FileSystemStoryPackageLoader(new StoryPackageOptions{Root=root},hasher,validator),hasher);}
    private static string FindStories(){var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!Directory.Exists(Path.Combine(directory.FullName,"stories")))directory=directory.Parent;return Path.Combine(directory!.FullName,"stories");}
    private static CommandContext C(Guid? team=null){var id=Guid.NewGuid();return new(id,id,id,team,new DateTimeOffset(2026,8,4,8,0,0,TimeSpan.Zero));}
}
