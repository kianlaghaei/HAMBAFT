using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;

namespace Hambaft.UnitTests;

public sealed class EffectEngineTests
{
    [Fact] public async Task Metric_change_and_set_apply_with_clamp_policy()
    {
        var x=await Context();var engine=new EffectEngine();var change=new EffectDefinition("test",EffectType.ChangeMetric,ConditionTargetScope.CurrentEntity,"entity.reliability",1000);
        var changed=engine.CreateEvents(change,x).Single().Should().BeOfType<MetricChanged>().Subject;changed.NewValue.Should().Be(100);x.State.Apply(changed);
        var set=engine.CreateEvents(new("set",EffectType.SetMetric,ConditionTargetScope.World,"world.pressure",-20),x with { NextStreamVersion=x.State.StateVersion+1 }).Single().Should().BeOfType<MetricSet>().Subject;set.NewValue.Should().Be(0);
    }
    [Fact] public async Task Memory_add_and_remove_are_meaningful()
    {
        var x=await Context();var engine=new EffectEngine();var added=engine.CreateEvents(new("add",EffectType.AddMemory,ConditionTargetScope.CurrentEntity,MemoryKey:"temporary",Visibility:MemoryVisibility.EntityPrivate),x).Single();added.Should().BeOfType<StoryMemoryAdded>();x.State.Apply(added);
        engine.CreateEvents(new("remove",EffectType.RemoveMemory,ConditionTargetScope.CurrentEntity,MemoryKey:"temporary"),x with { NextStreamVersion=x.State.StateVersion+1 }).Single().Should().BeOfType<StoryMemoryRemoved>();
    }
    [Fact] public async Task Relationship_change_is_directional()
    {
        var x=await Context();var e=new EffectDefinition("trust",EffectType.ChangeRelationship,ConditionTargetScope.RelationshipBetweenCurrentAndTarget,Value:7,TargetEntityDefinitionId:"carrier",RelationshipKey:"relationship.trust");var changed=new EffectEngine().CreateEvents(e,x).Single().Should().BeOfType<RelationshipChanged>().Subject;
        changed.SourceEntityId.Should().Be(x.Assignment!.TargetEntityId!.Value);changed.TargetEntityId.Should().NotBe(changed.SourceEntityId);changed.NewValue.Should().Be(7);
    }
    [Fact] public async Task Storylet_assignment_effect_creates_typed_assignment()
    {
        var x=await Context();var e=new EffectDefinition("assign",EffectType.AssignStorylet,StoryletId:"supplier-delay-response");new EffectEngine().CreateEvents(e,x).Single().Should().BeOfType<StoryletAssigned>();
    }
    [Fact] public async Task Public_narrative_effect_creates_publication()
    {
        var x=await Context();var e=new EffectDefinition("publish",EffectType.PublishWorldNarrative,StoryletId:"cargo-delay-coordinated-outcome");new EffectEngine().CreateEvents(e,x).Single().Should().BeOfType<WorldNarrativePublished>();
    }
    [Fact] public async Task Unknown_target_is_rejected()
    {
        var x=await Context();var e=new EffectDefinition("bad",EffectType.ChangeRelationship,ConditionTargetScope.RelationshipBetweenCurrentAndTarget,Value:1,TargetEntityDefinitionId:"missing",RelationshipKey:"relationship.trust");FluentActions.Invoking(()=>new EffectEngine().CreateEvents(e,x)).Should().Throw<DomainException>();
    }
    [Fact] public async Task Private_to_public_leak_is_rejected()
    {
        var x=await Context();var e=new EffectDefinition("leak",EffectType.PublishWorldNarrative,StoryletId:"supplier-delay-response");FluentActions.Invoking(()=>new EffectEngine().CreateEvents(e,x)).Should().Throw<DomainException>();
    }
    private static async Task<EffectExecutionContext> Context()
    {
        var s=await StoryRuntimeTestSupport.CreateStartedAsync();await s.Runtime.ExecuteAsync(new InitializeNarrative(s.SessionId,8,StoryRuntimeTestSupport.Context()),default);var state=StorySession.From(s.Store.Events);var assignment=state.StoryletAssignments.Single(x=>x.StoryletId=="supplier-delay-response");var storylet=s.Package.Storylets.Single(x=>x.Id==assignment.StoryletId);return new(state,s.Package,assignment,storylet,null,StoryRuntimeTestSupport.Context(s.SupplierTeamId).For(s.SessionId),state.StateVersion+1);
    }
}
