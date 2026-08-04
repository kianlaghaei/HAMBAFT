using FluentAssertions;
using Hambaft.Application;
using Hambaft.Contracts;
using Hambaft.Domain;

namespace Hambaft.UnitTests;

public sealed class EndingResolverTests
{
    [Fact]
    public void SignalR_ending_contract_contains_no_private_evidence_or_text()
    {
        typeof(EndingNotification).GetProperties().Select(x=>x.Name).Should().BeEquivalentTo(
            ["SessionId","ScopeId","EndingDefinitionId","StateVersion","EventType"]);
    }

    [Fact]
    public async Task Entity_priority_is_deterministic_and_evidence_is_structured()
    {
        var (package,state,history,entities)=await State();var bakery=entities[0];
        Add(state,history,new StoryMemoryAdded(MemoryScope.Entity,bakery.Id,"avan_accepted",null,MemoryVisibility.Public,"test",Meta(state.Id)));
        var resolver=new DeterministicEndingResolver(new ConditionEngine());
        var first=((IEntityEndingResolver)resolver).Resolve(package,state,bakery,history,state.StateVersion+1,Meta(state.Id).OccurredAtUtc);
        var second=((IEntityEndingResolver)resolver).Resolve(package,state,bakery,history,state.StateVersion+1,Meta(state.Id).OccurredAtUtc);
        first.EndingDefinitionId.Should().Be("bread-under-avan");first.Should().BeEquivalentTo(second);
        first.Evidence.Should().Contain(x=>x.Kind==EndingEvidenceKind.Memory&&x.Key=="avan_accepted");
        first.InputFingerprint.Should().StartWith("sha256:");
    }

    [Fact]
    public async Task Hidden_world_ending_uses_typed_aggregate_conditions()
    {
        var (package,state,history,entities)=await State();
        foreach(var entity in entities.Take(3))Add(state,history,new StoryMemoryAdded(MemoryScope.Entity,entity.Id,"fragment_disclosed",null,MemoryVisibility.Public,"test",Meta(state.Id)));
        Add(state,history,new MetricSet(MetricScope.World,state.Id,"Transparency",45,80,"test",Meta(state.Id)));
        var result=((IWorldEndingResolver)new DeterministicEndingResolver(new ConditionEngine())).Resolve(package,state,history,state.StateVersion+1,Meta(state.Id).OccurredAtUtc);
        result.EndingDefinitionId.Should().Be("the-open-ledger");
        result.Evidence.Should().Contain(x=>x.Kind==EndingEvidenceKind.MetricSnapshot&&x.Key=="Transparency");
    }

    [Fact]
    public async Task World_fallback_and_weighted_ties_are_stable()
    {
        var (package,state,history,_)=await State();var resolver=(IWorldEndingResolver)new DeterministicEndingResolver(new ConditionEngine());
        resolver.Resolve(package,state,history,state.StateVersion+1,Meta(state.Id).OccurredAtUtc).EndingDefinitionId.Should().Be("islands-of-light");
        var tied=package with { WorldEndings=[
            new("alpha","ending-world-islands-title","ending-world-islands-body",[],10,1,[],new Dictionary<string,string>()),
            new("beta","ending-world-islands-title","ending-world-islands-body",[],10,3,[],new Dictionary<string,string>())] };
        var a=resolver.Resolve(tied,state,history,state.StateVersion+1,Meta(state.Id).OccurredAtUtc);var b=resolver.Resolve(tied,state,history,state.StateVersion+1,Meta(state.Id).OccurredAtUtc);
        a.EndingDefinitionId.Should().Be(b.EndingDefinitionId);
    }

    [Fact]
    public async Task Domain_requires_all_entity_endings_before_world_and_cannot_resolve_twice()
    {
        var (package,state,history,entities)=await State();var resolver=new DeterministicEndingResolver(new ConditionEngine());
        foreach(var entity in entities.Where(x=>x.ControllerType==ControllerType.HumanTeam))
        {
            var result=((IEntityEndingResolver)resolver).Resolve(package,state,entity,history,state.StateVersion+1,Meta(state.Id).OccurredAtUtc);state.Apply(state.ResolveEntityEnding(result,Meta(state.Id)));
        }
        var world=((IWorldEndingResolver)resolver).Resolve(package,state,history,state.StateVersion+1,Meta(state.Id).OccurredAtUtc);state.Apply(state.ResolveWorldEnding(world,Meta(state.Id)));
        FluentActions.Invoking(()=>state.ResolveWorldEnding(world,Meta(state.Id))).Should().Throw<DomainException>();
        state.Apply(state.Complete(Meta(state.Id)));state.Status.Should().Be(SessionStatus.Completed);
    }

    private static async Task<(StoryPackage Package,StorySession State,List<IDomainEvent> History,List<WorldEntity> Entities)> State()
    {
        var package=await StoryRuntimeTestSupport.PackageServices().Loader.LoadAsync("hezar-cheragh","0.1.0",default);var history=new List<IDomainEvent>();
        var sessionId=Guid.Parse("71000000-0000-0000-0000-000000000001");var created=StorySession.Create(sessionId,package.Manifest.Id,package.Manifest.Version,package.ContentHash,42,Meta(sessionId));history.Add(created);var state=StorySession.From(history);
        var definitions=package.Entities.ToList();
        for(var i=0;i<definitions.Count;i++)
        {
            var teamId=DeterministicIds.Create("ending-team",i);var entityId=DeterministicIds.Create("ending-entity",i);
            Add(state,history,state.AddTeam(teamId,$"Team {i}","hash",Meta(sessionId)));
            Add(state,history,state.CreateWorldEntity(entityId,definitions[i].Id,definitions[i].DisplayName,ControllerType.HumanTeam,null,Meta(sessionId)));
            Add(state,history,state.AssignEntityToTeam(entityId,teamId,Meta(sessionId)));
        }
        Add(state,history,state.Start(Meta(sessionId)));Add(state,history,state.InitializeNarrative(package.Manifest.Id,package.Manifest.Version,package.ContentHash,"slice-complete",Meta(sessionId)));
        foreach(var metric in package.Metrics.Where(x=>x.Scope==MetricScope.World))Add(state,history,new MetricSet(MetricScope.World,state.Id,metric.Key,metric.DefaultValue,metric.DefaultValue,"test",Meta(sessionId)));
        foreach(var entity in state.Entities)foreach(var metric in package.Metrics.Where(x=>x.Scope==MetricScope.Entity)){var value=package.Entities.Single(x=>x.Id==entity.DefinitionId).InitialMetrics[metric.Key];Add(state,history,new MetricSet(MetricScope.Entity,entity.Id,metric.Key,metric.DefaultValue,value,"test",Meta(sessionId)));}
        return(package,state,history,state.Entities.ToList());
    }
    private static void Add(StorySession state,List<IDomainEvent> history,IDomainEvent e){state.Apply(e);history.Add(e);}
    private static EventMetadata Meta(Guid sessionId)=>new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),Guid.NewGuid(),sessionId,null,new DateTimeOffset(2026,8,4,12,0,0,TimeSpan.Zero));
}
