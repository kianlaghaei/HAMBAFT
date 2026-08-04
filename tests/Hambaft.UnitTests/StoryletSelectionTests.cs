using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;

namespace Hambaft.UnitTests;

public sealed class StoryletSelectionTests
{
    [Fact] public async Task Highest_priority_eligible_storylet_wins()
    {
        var(s,p,state)=await Data();var low=Candidate("low",1);var high=Candidate("high",10);var selected=s.Select(p with { Storylets=[low,high] },state,World());selected!.Id.Should().Be("high");
    }
    [Fact] public async Task Weighted_selection_is_stable_and_source_order_independent()
    {
        var(s,p,state)=await Data();var a=Candidate("a",10,1);var b=Candidate("b",10,4);var context=World();var first=s.Select(p with { Storylets=[a,b] },state,context);var reversed=s.Select(p with { Storylets=[b,a] },state,context);reversed!.Id.Should().Be(first!.Id);
    }
    [Fact] public async Task Repeat_policy_once_per_session_excludes_prior_assignment()
    {
        var scenario=await StoryRuntimeTestSupport.CreateStartedAsync();var initialized=await scenario.Runtime.ExecuteAsync(new InitializeNarrative(scenario.SessionId,8,StoryRuntimeTestSupport.Context()),default);var state=StorySession.From(scenario.Store.Events);var selected=new DeterministicStoryletSelector(new ConditionEngine()).Select(scenario.Package,state,new("response",StoryletScope.TeamPrivate,scenario.SupplierTeamId,scenario.SupplierEntityId,initialized.StateVersion));selected.Should().BeNull();
    }
    [Fact] public async Task False_conditions_are_filtered()
    {
        var(s,p,state)=await Data();var falseOne=Candidate("false",20) with { Conditions=[new(ConditionType.CheckpointIs,CheckpointId:"never")] };var trueOne=Candidate("true",10);s.Select(p with { Storylets=[falseOne,trueOne] },state,World())!.Id.Should().Be("true");
    }
    [Fact] public async Task Entity_compatibility_filters_team_target()
    {
        var scenario=await StoryRuntimeTestSupport.CreateStartedAsync();var selector=new DeterministicStoryletSelector(new ConditionEngine());var selected=selector.Select(scenario.Package,StorySession.From(scenario.Store.Events),new("response",StoryletScope.TeamPrivate,scenario.SupplierTeamId,scenario.SupplierEntityId,8));selected!.Id.Should().Be("supplier-delay-response");
    }
    [Fact] public async Task Seed_is_stable_and_can_select_another_valid_weighted_storylet()
    {
        var(s,p,_)=await Data();var candidates=Enumerable.Range(1,4).Select(i=>Candidate($"weighted-{i}",10,1)).ToList();var package=p with { Storylets=candidates };var results=new HashSet<string>();
        for(var seed=1;seed<=40;seed++){var id=Guid.Parse("10000000-0000-0000-0000-000000000001");var created=StorySession.Create(id,p.Manifest.Id,p.Manifest.Version,p.ContentHash,seed,StoryRuntimeTestSupport.Context().For(id));var state=StorySession.From([created]);var first=s.Select(package,state,World())!.Id;var replay=s.Select(package,StorySession.From([created]),World())!.Id;replay.Should().Be(first);results.Add(first);}
        results.Count.Should().BeGreaterThan(1);
    }
    private static StoryletDefinition Candidate(string id,int priority,int weight=1)=>new(id,"weighted",StoryletScope.WorldPublic,new(TargetSelectorType.World),"cargo-delay-opening",[],[],priority,weight,RepeatPolicy.Repeatable,false);
    private static StoryletSelectionContext World()=>new("weighted",StoryletScope.WorldPublic,null,null,1);
    private static async Task<(IStoryletSelector,StoryPackage,StorySession)> Data(){var services=StoryRuntimeTestSupport.PackageServices();var package=await services.Loader.LoadAsync("sample-cargo-delay","1.0.0",default);var id=Guid.Parse("10000000-0000-0000-0000-000000000001");var created=StorySession.Create(id,package.Manifest.Id,package.Manifest.Version,package.ContentHash,42,StoryRuntimeTestSupport.Context().For(id));return(new DeterministicStoryletSelector(new ConditionEngine()),package,StorySession.From([created]));}
}
