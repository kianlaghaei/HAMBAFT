using Hambaft.Application;
using Hambaft.Domain;
using Hambaft.Infrastructure.Security;
using Hambaft.Infrastructure.StoryPackages;

namespace Hambaft.UnitTests;

internal static class StoryRuntimeTestSupport
{
    public static string StoriesRoot
    {
        get
        {
            var directory=new DirectoryInfo(AppContext.BaseDirectory);
            while(directory is not null&&!Directory.Exists(Path.Combine(directory.FullName,"stories"))) directory=directory.Parent;
            return Path.Combine(directory?.FullName??throw new InvalidOperationException("Repository root not found."),"stories");
        }
    }
    public static (IStoryPackageLoader Loader,IStoryPackageValidator Validator,IStoryPackageHasher Hasher) PackageServices()
    {
        var hasher=new DeterministicStoryPackageHasher();var validator=new StoryPackageValidator();var loader=new FileSystemStoryPackageLoader(new StoryPackageOptions{Root=StoriesRoot},hasher,validator);return(loader,validator,hasher);
    }
    public static SessionRuntime Runtime(InMemorySessionStore store,IStoryPackageLoader loader)=>new(store,new PairingCodeGenerator(),loader,new DeterministicStoryletSelector(new ConditionEngine()),new EffectEngine(),new DeterministicBehaviorResolver(new ConditionEngine()));
    public static CommandContext Context(Guid? team=null,Guid? command=null)=>new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),command??Guid.NewGuid(),command??Guid.NewGuid(),team,new DateTimeOffset(2026,8,4,8,0,0,TimeSpan.Zero));
    public static async Task<Scenario> CreateStartedAsync()
    {
        var services=PackageServices();var package=await services.Loader.LoadAsync("sample-cargo-delay","1.0.0",default);var store=new InMemorySessionStore();var runtime=Runtime(store,services.Loader);
        var session=Guid.Parse("10000000-0000-0000-0000-000000000001");var supplierTeam=Guid.Parse("20000000-0000-0000-0000-000000000001");var carrierTeam=Guid.Parse("20000000-0000-0000-0000-000000000002");var supplier=Guid.Parse("30000000-0000-0000-0000-000000000001");var carrier=Guid.Parse("30000000-0000-0000-0000-000000000002");
        await runtime.ExecuteAsync(new CreateSession(session,package.Manifest.Id,package.Manifest.Version,package.ContentHash,42,0,Context()),default);
        await runtime.ExecuteAsync(new AddTeam(session,supplierTeam,"Supplier",1,Context()),default);await runtime.ExecuteAsync(new AddTeam(session,carrierTeam,"Carrier",2,Context()),default);
        await runtime.ExecuteAsync(new CreateWorldEntity(session,supplier,"supplier","Supplier",ControllerType.HumanTeam,null,3,Context()),default);await runtime.ExecuteAsync(new CreateWorldEntity(session,carrier,"carrier","Carrier",ControllerType.HumanTeam,null,4,Context()),default);
        await runtime.ExecuteAsync(new AssignEntityToTeam(session,supplier,supplierTeam,5,Context()),default);await runtime.ExecuteAsync(new AssignEntityToTeam(session,carrier,carrierTeam,6,Context()),default);await runtime.ExecuteAsync(new StartSession(session,7,Context()),default);
        return new(store,runtime,package,session,supplierTeam,carrierTeam,supplier,carrier);
    }
    public static async Task<Phase3Scenario> CreatePhase3StartedAsync(string difficulty="standard",int seed=42,string behaviorProfile="cooperative-credit")
    {
        var services=PackageServices();var package=await services.Loader.LoadAsync("sample-cargo-delay","1.1.0",default);var store=new InMemorySessionStore();var runtime=new SessionRuntime(store,new FixedTestPairing(),services.Loader,new DeterministicStoryletSelector(new ConditionEngine()),new EffectEngine(),new DeterministicBehaviorResolver(new ConditionEngine()));var sequence=0;CommandContext Next()=>Context(command:DeterministicIds.Create("phase3-setup",++sequence));
        var session=Guid.Parse("11000000-0000-0000-0000-000000000001");var supplierTeam=Guid.Parse("21000000-0000-0000-0000-000000000001");var carrierTeam=Guid.Parse("21000000-0000-0000-0000-000000000002");var supplier=Guid.Parse("31000000-0000-0000-0000-000000000001");var carrier=Guid.Parse("31000000-0000-0000-0000-000000000002");var credit=Guid.Parse("31000000-0000-0000-0000-000000000003");
        await runtime.ExecuteAsync(new CreateSession(session,package.Manifest.Id,package.Manifest.Version,package.ContentHash,seed,0,Next(),difficulty),default);
        await runtime.ExecuteAsync(new AddTeam(session,supplierTeam,"Supplier",1,Next()),default);await runtime.ExecuteAsync(new AddTeam(session,carrierTeam,"Carrier",2,Next()),default);
        await runtime.ExecuteAsync(new CreateWorldEntity(session,supplier,"supplier","Supplier",ControllerType.HumanTeam,null,3,Next()),default);await runtime.ExecuteAsync(new CreateWorldEntity(session,carrier,"carrier","Carrier",ControllerType.HumanTeam,null,4,Next()),default);await runtime.ExecuteAsync(new CreateWorldEntity(session,credit,"credit-provider","Credit Provider",ControllerType.AuthoredBehavior,behaviorProfile,5,Next()),default);
        await runtime.ExecuteAsync(new AssignEntityToTeam(session,supplier,supplierTeam,6,Next()),default);await runtime.ExecuteAsync(new AssignEntityToTeam(session,carrier,carrierTeam,7,Next()),default);await runtime.ExecuteAsync(new StartSession(session,8,Next()),default);
        var initialized=await runtime.ExecuteAsync(new InitializeNarrative(session,9,Next()),default);
        return new(store,runtime,package,session,supplierTeam,carrierTeam,supplier,carrier,credit,initialized.StateVersion);
    }
}

internal sealed record Scenario(InMemorySessionStore Store,SessionRuntime Runtime,StoryPackage Package,Guid SessionId,Guid SupplierTeamId,Guid CarrierTeamId,Guid SupplierEntityId,Guid CarrierEntityId);
internal sealed record Phase3Scenario(InMemorySessionStore Store,SessionRuntime Runtime,StoryPackage Package,Guid SessionId,Guid SupplierTeamId,Guid CarrierTeamId,Guid SupplierEntityId,Guid CarrierEntityId,Guid CreditEntityId,long Version);

internal sealed class FixedTestPairing : IPairingCodeGenerator
{
    public PairingCodeMaterial Generate()=>new("PHASE3","fixed-phase3-hash");
    public bool Verify(string rawCode,string persistedHash)=>true;
}

internal sealed class InMemorySessionStore : ISessionStore
{
    public List<IDomainEvent> Events { get; }=[];
    public Task<StorySession?> LoadAsync(Guid sessionId,CancellationToken cancellationToken)=>Task.FromResult(Events.Count==0?null:StorySession.From(Events));
    public Task<IReadOnlyList<IDomainEvent>> LoadEventsAsync(Guid sessionId,CancellationToken cancellationToken)=>Task.FromResult<IReadOnlyList<IDomainEvent>>(Events.ToList());
    public Task AppendAsync(Guid sessionId,long expectedVersion,IReadOnlyList<IDomainEvent> events,StorySession projectedState,CancellationToken cancellationToken)
    {
        if(Events.Count!=expectedVersion) throw new ConcurrencyConflictException(sessionId,expectedVersion,Events.Count); Events.AddRange(events); return Task.CompletedTask;
    }
    public Task<SessionStateView?> LoadSessionViewAsync(Guid sessionId,CancellationToken cancellationToken)=>Task.FromResult(Events.Count==0?null:ViewProjector.Session(StorySession.From(Events)));
    public Task<PublicWorldView?> LoadPublicViewAsync(Guid sessionId,CancellationToken cancellationToken)=>Task.FromResult(Events.Count==0?null:ViewProjector.Public(StorySession.From(Events)));
    public Task<TeamExperienceView?> LoadTeamViewAsync(Guid teamId,CancellationToken cancellationToken)
    {
        if(Events.Count==0)return Task.FromResult<TeamExperienceView?>(null);var state=StorySession.From(Events);var team=state.Teams.SingleOrDefault(x=>x.Id==teamId);return Task.FromResult(team is null?null:ViewProjector.Team(state,team));
    }
}
