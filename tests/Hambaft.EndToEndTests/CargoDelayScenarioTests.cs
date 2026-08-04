using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;
using Hambaft.Infrastructure.StoryPackages;

namespace Hambaft.EndToEndTests;

public sealed class CargoDelayScenarioTests
{
    [Fact]
    public async Task Complete_two_team_sample_replays_twice_to_identical_state_and_fingerprint()
    {
        var first=await Run();var second=await Run();first.State.Should().BeEquivalentTo(second.State);first.Fingerprint.Should().Be(second.Fingerprint);StorySession.From(first.Events).Should().BeEquivalentTo(first.State);
        first.State.CurrentCheckpointId.Should().Be("outcome");first.State.CurrentWorldStoryletId.Should().Be("cargo-delay-coordinated-outcome");first.State.Memories.Should().Contain(x=>x.Key=="supplier_disclosed_delay").And.Contain(x=>x.Key=="carrier_reserved_capacity");first.State.Relationships.Should().Contain(x=>x.RelationshipKey=="relationship.trust"&&x.NumericValue>0);
        first.SupplierView.PrivateStorylets.Should().ContainSingle(x=>x.StoryletId=="supplier-delay-response");first.SupplierView.PrivateStorylets.Should().NotContain(x=>x.StoryletId=="carrier-delay-response");first.PublicView.Narrative!.StoryletId.Should().Be("cargo-delay-coordinated-outcome");
    }
    private static async Task<Result> Run()
    {
        var hasher=new DeterministicStoryPackageHasher();var validator=new StoryPackageValidator();var loader=new FileSystemStoryPackageLoader(new StoryPackageOptions{Root=FindStories()},hasher,validator);var package=await loader.LoadAsync("sample-cargo-delay","1.0.0",default);var store=new MemoryStore();var runtime=new SessionRuntime(store,new FixedPairing(),loader,new DeterministicStoryletSelector(new ConditionEngine()),new EffectEngine());var commands=new Contexts();
        var session=Guid.Parse("10000000-0000-0000-0000-000000000001");var supplierTeam=Guid.Parse("20000000-0000-0000-0000-000000000001");var carrierTeam=Guid.Parse("20000000-0000-0000-0000-000000000002");var supplier=Guid.Parse("30000000-0000-0000-0000-000000000001");var carrier=Guid.Parse("30000000-0000-0000-0000-000000000002");
        await runtime.ExecuteAsync(new CreateSession(session,package.Manifest.Id,package.Manifest.Version,package.ContentHash,42,0,commands.Next()),default);await runtime.ExecuteAsync(new AddTeam(session,supplierTeam,"Supplier",1,commands.Next()),default);await runtime.ExecuteAsync(new AddTeam(session,carrierTeam,"Carrier",2,commands.Next()),default);await runtime.ExecuteAsync(new CreateWorldEntity(session,supplier,"supplier","Supplier",ControllerType.HumanTeam,null,3,commands.Next()),default);await runtime.ExecuteAsync(new CreateWorldEntity(session,carrier,"carrier","Carrier",ControllerType.HumanTeam,null,4,commands.Next()),default);await runtime.ExecuteAsync(new AssignEntityToTeam(session,supplier,supplierTeam,5,commands.Next()),default);await runtime.ExecuteAsync(new AssignEntityToTeam(session,carrier,carrierTeam,6,commands.Next()),default);await runtime.ExecuteAsync(new StartSession(session,7,commands.Next()),default);
        var initialized=await runtime.ExecuteAsync(new InitializeNarrative(session,8,commands.Next()),default);var state=StorySession.From(store.Events);var supplierView=ViewProjector.Team(state,state.Teams.Single(x=>x.Id==supplierTeam),package);var supplierAssignment=state.StoryletAssignments.Single(x=>x.StoryletId=="supplier-delay-response");var carrierAssignment=state.StoryletAssignments.Single(x=>x.StoryletId=="carrier-delay-response");var first=await runtime.ExecuteAsync(new SubmitStoryChoice(session,supplierAssignment.AssignmentId,"disclose-full-delay",initialized.StateVersion,commands.Next(supplierTeam)),default);var second=await runtime.ExecuteAsync(new SubmitStoryChoice(session,carrierAssignment.AssignmentId,"reserve-emergency-capacity",first.StateVersion,commands.Next(carrierTeam)),default);await runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(session,second.StateVersion,commands.Next()),default);
        state=StorySession.From(store.Events);var publicView=ViewProjector.Hydrate(ViewProjector.Public(state),package);return new(state,store.Events.ToList(),EventFingerprint.Compute(store.Events),supplierView,publicView);
    }
    private static string FindStories(){var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!Directory.Exists(Path.Combine(directory.FullName,"stories")))directory=directory.Parent;return Path.Combine(directory!.FullName,"stories");}
    private sealed record Result(StorySession State,IReadOnlyList<IDomainEvent> Events,string Fingerprint,TeamExperienceView SupplierView,PublicWorldView PublicView);
    private sealed class Contexts{private int value;public CommandContext Next(Guid? team=null){var command=DeterministicIds.Create("command",++value);return new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),command,command,team,new DateTimeOffset(2026,8,4,8,0,0,TimeSpan.Zero));}}
    private sealed class FixedPairing : IPairingCodeGenerator{public PairingCodeMaterial Generate()=>new("PAIRCODE","fixed-hash");public bool Verify(string rawCode,string persistedHash)=>true;}
    private sealed class MemoryStore : ISessionStore
    {
        public List<IDomainEvent> Events{get;}=[];public Task<StorySession?> LoadAsync(Guid id,CancellationToken ct)=>Task.FromResult(Events.Count==0?null:StorySession.From(Events));public Task<IReadOnlyList<IDomainEvent>> LoadEventsAsync(Guid id,CancellationToken ct)=>Task.FromResult<IReadOnlyList<IDomainEvent>>(Events.ToList());public Task AppendAsync(Guid id,long expected,IReadOnlyList<IDomainEvent> events,StorySession state,CancellationToken ct){if(expected!=Events.Count)throw new ConcurrencyConflictException(id,expected,Events.Count);Events.AddRange(events);return Task.CompletedTask;}public Task<SessionStateView?> LoadSessionViewAsync(Guid id,CancellationToken ct)=>Task.FromResult<SessionStateView?>(null);public Task<PublicWorldView?> LoadPublicViewAsync(Guid id,CancellationToken ct)=>Task.FromResult<PublicWorldView?>(null);public Task<TeamExperienceView?> LoadTeamViewAsync(Guid id,CancellationToken ct)=>Task.FromResult<TeamExperienceView?>(null);
    }
}
