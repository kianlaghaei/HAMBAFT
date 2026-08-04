using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;
using Hambaft.Infrastructure.StoryPackages;

namespace Hambaft.EndToEndTests;

public sealed class HezarCheraghPhase4ScenarioTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Complete_vertical_slice_supports_team_counts(int teamCount)
    {
        var result=await Run(teamCount,"standard",42);
        result.State.Status.Should().Be(SessionStatus.Completed);
        result.State.EndingResults.Count(x=>x.Scope==EndingScope.Entity).Should().Be(teamCount);
        result.State.EndingResults.Should().ContainSingle(x=>x.Scope==EndingScope.World);
        result.State.Entities.Count(x=>x.ControllerType==ControllerType.AuthoredBehavior).Should().Be(4-teamCount);
        result.State.AuthoredBehaviorSelections.Should().HaveCount((4-teamCount)*4);
        result.State.Proposals.Should().Contain(x=>x.Status==ProposalStatus.Executed).And.Contain(x=>x.Status==ProposalStatus.Expired);
        result.State.Agreements.Should().ContainSingle(x=>x.Status==AgreementStatus.Executed);
        result.State.ScheduledConsequences.Should().OnlyContain(x=>x.Status==ScheduledConsequenceStatus.Triggered);
    }

    [Fact]
    public async Task Same_seed_package_and_history_shape_replay_identically()
    {
        var first=await Run(2,"standard",147);var second=await Run(2,"standard",147);
        first.Fingerprint.Should().Be(second.Fingerprint);
        first.Assignments.Should().Equal(second.Assignments);
        first.BehaviorActions.Should().Equal(second.BehaviorActions);
        first.Endings.Should().Equal(second.Endings);
        first.State.EndingResults.Select(x=>x.InputFingerprint).Should().Equal(second.State.EndingResults.Select(x=>x.InputFingerprint));
        JsonSerializer.Serialize(StorySession.From(first.Events)).Should().Be(JsonSerializer.Serialize(first.State));
        var timer=Stopwatch.StartNew();for(var i=0;i<20;i++)_=StorySession.From(first.Events);timer.Stop();timer.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Hard_mode_is_deterministic_and_increases_self_interested_weight()
    {
        var first=await Run(2,"hard",91);var second=await Run(2,"hard",91);
        first.BehaviorActions.Should().Equal(second.BehaviorActions);
        first.State.AuthoredBehaviorSelections.Should().OnlyContain(x=>x.DifficultyId=="hard");
        first.Package.DifficultyDefinitions.Single(x=>x.Id=="hard").BehaviorRuleWeightModifiers.Should().Contain(x=>x.RuleId=="opportunistic-rule"&&x.Multiplier>1);
    }

    private static async Task<Result> Run(int teamCount,string difficulty,int seed)
    {
        var stories=FindStories();var loader=new FileSystemStoryPackageLoader(new StoryPackageOptions{Root=stories},new DeterministicStoryPackageHasher(),new StoryPackageValidator());
        var package=await loader.LoadAsync("hezar-cheragh","0.1.0",default);var store=new MemoryStore();var conditions=new ConditionEngine();
        var ending=new DeterministicEndingResolver(conditions);var runtime=new SessionRuntime(store,new FixedPairing(),loader,new DeterministicStoryletSelector(conditions),new EffectEngine(),new DeterministicBehaviorResolver(conditions),ending,ending);
        var ids=new Ids(teamCount,difficulty,seed);var contexts=new Contexts(teamCount,difficulty,seed);long version=0;
        version=(await runtime.ExecuteAsync(new CreateSession(ids.Session,package.Manifest.Id,package.Manifest.Version,package.ContentHash,seed,version,contexts.Next(),difficulty),default)).StateVersion;
        for(var i=0;i<teamCount;i++)version=(await runtime.ExecuteAsync(new AddTeam(ids.Session,ids.Teams[i],$"Team {i+1}",version,contexts.Next()),default)).StateVersion;
        for(var i=0;i<4;i++)
        {
            var controlled=i<teamCount;
            version=controlled
                ?(await runtime.ExecuteAsync(new CreateWorldEntity(ids.Session,ids.Entities[i],package.Entities[i].Id,package.Entities[i].DisplayName,ControllerType.HumanTeam,null,version,contexts.Next()),default)).StateVersion
                :(await runtime.CreateEntityAsync(ids.Session,ids.Entities[i],package.Entities[i].Id,package.Entities[i].DisplayName,nameof(ControllerType.AuthoredBehavior),null,version,contexts.Next(),default)).StateVersion;
        }
        for(var i=0;i<teamCount;i++)version=(await runtime.ExecuteAsync(new AssignEntityToTeam(ids.Session,ids.Entities[i],ids.Teams[i],version,contexts.Next()),default)).StateVersion;
        version=(await runtime.ExecuteAsync(new StartSession(ids.Session,version,contexts.Next()),default)).StateVersion;
        version=(await runtime.ExecuteAsync(new InitializeNarrative(ids.Session,version,contexts.Next()),default)).StateVersion;

        version=await SubmitCheckpoint(runtime,store,ids,contexts,version,"morning-without-bell",new[]{"bakery-disclose-fragment","logistics-share-fragment","printing-disclose-fragment","exchange-hold-fragment"});
        version=(await runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(ids.Session,version,contexts.Next()),default)).StateVersion;
        StorySession.From(store.Events).CurrentCheckpointId.Should().Be("cargo-did-not-arrive");

        version=await SubmitCheckpoint(runtime,store,ids,contexts,version,"cargo-did-not-arrive",new[]{"bakery-stable-price","logistics-use-hidden-route","printing-publish-evidence","exchange-extend-emergency-credit"});
        var acceptedId=DeterministicIds.Create(ids.Session,"accepted-proposal");
        version=(await runtime.ExecuteAsync(new SendProposal(ids.Session,acceptedId,"emergency-supply",ids.Teams[1],Terms(8,true),null,version,contexts.Next(ids.Teams[0])),default)).StateVersion;
        version=(await runtime.ExecuteAsync(new CounterProposal(ids.Session,acceptedId,1,Terms(10,false),version,contexts.Next(ids.Teams[1])),default)).StateVersion;
        version=(await runtime.ExecuteAsync(new AcceptProposal(ids.Session,acceptedId,2,version,contexts.Next(ids.Teams[0])),default)).StateVersion;
        var expiringId=DeterministicIds.Create(ids.Session,"expiring-proposal");
        version=(await runtime.ExecuteAsync(new SendProposal(ids.Session,expiringId,"credit-guarantee",ids.Teams[1],Terms(4,true),null,version,contexts.Next(ids.Teams[0])),default)).StateVersion;
        version=(await runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(ids.Session,version,contexts.Next()),default)).StateVersion;

        version=await SubmitCheckpoint(runtime,store,ids,contexts,version,"avan-offer",new[]{"bakery-decline-avan","logistics-accept-avan","printing-decline-avan","exchange-accept-avan"});
        version=(await runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(ids.Session,version,contexts.Next()),default)).StateVersion;
        version=await SubmitCheckpoint(runtime,store,ids,contexts,version,"market-gathering",new[]{"bakery-market-cooperation","logistics-market-cooperation","printing-independent-direction","exchange-market-cooperation"});
        version=(await runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(ids.Session,version,contexts.Next()),default)).StateVersion;
        var completed=await runtime.ExecuteAsync(new ResolveEndings(ids.Session,version,contexts.Next()),default);version=completed.StateVersion;

        var state=StorySession.From(store.Events);state.StateVersion.Should().Be(version);state.CurrentCheckpointId.Should().Be("slice-complete");
        return new(package,state,store.Events.ToList(),EventFingerprint.Compute(store.Events),state.StoryletAssignments.Select(x=>$"{x.CheckpointId}:{x.StoryletId}:{x.TargetTeamId}").ToList(),state.AuthoredBehaviorSelections.Select(x=>$"{x.EntityId}:{x.CheckpointId}:{x.ActionId}").ToList(),state.EndingResults.OrderBy(x=>x.Scope).ThenBy(x=>x.ScopeId).Select(x=>x.EndingDefinitionId).ToList());
    }

    private static async Task<long> SubmitCheckpoint(SessionRuntime runtime,MemoryStore store,Ids ids,Contexts contexts,long version,string checkpoint,IReadOnlyList<string> choices)
    {
        var state=StorySession.From(store.Events);state.CurrentCheckpointId.Should().Be(checkpoint);
        foreach(var assignment in state.StoryletAssignments.Where(x=>x.CheckpointId==checkpoint&&x.RequiredResponse&&x.Status==StoryletAssignmentStatus.Assigned).OrderBy(x=>x.TargetTeamId))
        {
            var teamIndex=ids.Teams.IndexOf(assignment.TargetTeamId!.Value);version=(await runtime.ExecuteAsync(new SubmitStoryChoice(ids.Session,assignment.AssignmentId,choices[teamIndex],version,contexts.Next(assignment.TargetTeamId)),default)).StateVersion;
        }
        return version;
    }
    private static JsonElement Terms(decimal units,bool isPublic)=>JsonDocument.Parse($$"""{"units":{{units}},"public":{{isPublic.ToString().ToLowerInvariant()}}}""").RootElement.Clone();
    private static string FindStories(){var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!Directory.Exists(Path.Combine(directory.FullName,"stories")))directory=directory.Parent;return Path.Combine(directory!.FullName,"stories");}

    private sealed record Result(StoryPackage Package,StorySession State,IReadOnlyList<IDomainEvent> Events,string Fingerprint,IReadOnlyList<string> Assignments,IReadOnlyList<string> BehaviorActions,IReadOnlyList<string> Endings);
    private sealed class Ids
    {
        public Guid Session{get;}public List<Guid> Teams{get;}public List<Guid> Entities{get;}
        public Ids(int count,string difficulty,int seed){Session=DeterministicIds.Create("hc-session",count,difficulty,seed);Teams=Enumerable.Range(0,count).Select(i=>DeterministicIds.Create(Session,"team",i)).ToList();Entities=Enumerable.Range(0,4).Select(i=>DeterministicIds.Create(Session,"entity",i)).ToList();}
    }
    private sealed class Contexts(int count,string difficulty,int seed)
    {
        private int value;public CommandContext Next(Guid? team=null){var command=DeterministicIds.Create("hc-command",count,difficulty,seed,++value);return new(DeterministicIds.Create("hc-correlation",count,difficulty,seed),command,command,team,new DateTimeOffset(2026,8,4,8,0,0,TimeSpan.Zero));}
    }
    private sealed class FixedPairing : IPairingCodeGenerator{public PairingCodeMaterial Generate()=>new("HEZAR01","hash");public bool Verify(string rawCode,string persistedHash)=>true;}
    private sealed class MemoryStore : ISessionStore
    {
        public List<IDomainEvent> Events{get;}=[];
        public Task<StorySession?> LoadAsync(Guid id,CancellationToken ct)=>Task.FromResult(Events.Count==0?null:StorySession.From(Events));
        public Task<IReadOnlyList<IDomainEvent>> LoadEventsAsync(Guid id,CancellationToken ct)=>Task.FromResult<IReadOnlyList<IDomainEvent>>(Events.ToList());
        public Task AppendAsync(Guid id,long expected,IReadOnlyList<IDomainEvent> events,StorySession state,CancellationToken ct){if(expected!=Events.Count)throw new ConcurrencyConflictException(id,expected,Events.Count);Events.AddRange(events);return Task.CompletedTask;}
        public Task<SessionStateView?> LoadSessionViewAsync(Guid id,CancellationToken ct)=>Task.FromResult(Events.Count==0?null:ViewProjector.Session(StorySession.From(Events)));
        public Task<PublicWorldView?> LoadPublicViewAsync(Guid id,CancellationToken ct)=>Task.FromResult(Events.Count==0?null:ViewProjector.Public(StorySession.From(Events)));
        public Task<TeamExperienceView?> LoadTeamViewAsync(Guid id,CancellationToken ct){var state=StorySession.From(Events);var team=state.Teams.SingleOrDefault(x=>x.Id==id);return Task.FromResult(team is null?null:ViewProjector.Team(state,team));}
        public Task<EndingEvidenceView?> LoadEndingEvidenceAsync(Guid id,CancellationToken ct){var state=StorySession.From(Events);return Task.FromResult<EndingEvidenceView?>(new(id,state.EndingResults.Where(x=>x.Scope==EndingScope.Entity).ToList(),state.EndingResults.SingleOrDefault(x=>x.Scope==EndingScope.World),state.StoryPackageId,state.StoryVersion,state.ContentHash,state.StateVersion));}
    }
}

internal static class GuidListExtensions
{
    public static int IndexOf(this IReadOnlyList<Guid> values,Guid value){for(var i=0;i<values.Count;i++)if(values[i]==value)return i;return -1;}
}
