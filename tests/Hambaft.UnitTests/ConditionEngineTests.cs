using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;

namespace Hambaft.UnitTests;

public sealed class ConditionEngineTests
{
    [Fact]
    public async Task Every_supported_condition_type_and_nested_composites_are_deterministic_and_traced()
    {
        var scenario=await ResolvedScenario();var state=StorySession.From(scenario.Store.Events);var supplierAssignment=state.StoryletAssignments.Single(x=>x.StoryletId=="supplier-delay-response");
        var context=new ConditionEvaluationContext(state,scenario.Package,scenario.SupplierTeamId,scenario.SupplierEntityId,supplierAssignment.AssignmentId);var engine=new ConditionEngine();
        var conditions=new[]
        {
            new ConditionDefinition(ConditionType.MetricAbove,Scope:ConditionTargetScope.World,MetricKey:"world.pressure",Expected:40),
            new ConditionDefinition(ConditionType.MetricAtLeast,Scope:ConditionTargetScope.World,MetricKey:"world.pressure",Expected:48),
            new ConditionDefinition(ConditionType.MetricBelow,Scope:ConditionTargetScope.World,MetricKey:"world.pressure",Expected:60),
            new ConditionDefinition(ConditionType.MetricAtMost,Scope:ConditionTargetScope.World,MetricKey:"world.pressure",Expected:48),
            new ConditionDefinition(ConditionType.MetricEquals,Scope:ConditionTargetScope.World,MetricKey:"world.pressure",Expected:48),
            new ConditionDefinition(ConditionType.MemoryExists,Scope:ConditionTargetScope.ExplicitEntityDefinition,MemoryKey:"supplier_disclosed_delay",EntityDefinitionId:"supplier"),
            new ConditionDefinition(ConditionType.MemoryMissing,Scope:ConditionTargetScope.CurrentEntity,MemoryKey:"not_present"),
            new ConditionDefinition(ConditionType.RelationshipAbove,Scope:ConditionTargetScope.RelationshipBetweenCurrentAndTarget,Expected:3,TargetEntityDefinitionId:"carrier",RelationshipKey:"relationship.trust"),
            new ConditionDefinition(ConditionType.RelationshipBelow,Scope:ConditionTargetScope.RelationshipBetweenCurrentAndTarget,Expected:5,TargetEntityDefinitionId:"carrier",RelationshipKey:"relationship.trust"),
            new ConditionDefinition(ConditionType.EntityControllerIs,Scope:ConditionTargetScope.CurrentEntity,Controller:ControllerType.HumanTeam),
            new ConditionDefinition(ConditionType.EntityDefinitionIs,Scope:ConditionTargetScope.CurrentEntity,EntityDefinitionId:"supplier"),
            new ConditionDefinition(ConditionType.TeamControlsEntityDefinition,Scope:ConditionTargetScope.CurrentTeam,EntityDefinitionId:"supplier"),
            new ConditionDefinition(ConditionType.SessionStatusIs,SessionStatus:SessionStatus.Running),
            new ConditionDefinition(ConditionType.CheckpointIs,CheckpointId:"outcome"),
            new ConditionDefinition(ConditionType.ChoiceWasSubmitted,ChoiceId:"disclose-full-delay"),
            new ConditionDefinition(ConditionType.All,Conditions:[new(ConditionType.SessionStatusIs,SessionStatus:SessionStatus.Running),new(ConditionType.CheckpointIs,CheckpointId:"outcome")]),
            new ConditionDefinition(ConditionType.Any,Conditions:[new(ConditionType.CheckpointIs,CheckpointId:"missing"),new(ConditionType.CheckpointIs,CheckpointId:"outcome")]),
            new ConditionDefinition(ConditionType.Not,Condition:new(ConditionType.CheckpointIs,CheckpointId:"missing"))
        };
        foreach(var condition in conditions)
        {
            var evaluation=engine.Evaluate(condition,context,true);evaluation.Result.Should().BeTrue($"{condition.Type} should match the prepared state");evaluation.Trace.Should().NotBeEmpty();
        }
        engine.Evaluate(conditions[0],context,true).Trace.Single().ResolvedTarget.Should().StartWith("world:");
        engine.Evaluate(new(ConditionType.MetricEquals,Scope:ConditionTargetScope.CurrentEntity,MetricKey:"entity.reliability",Expected:60),context,true).Result.Should().BeTrue();
    }

    private static async Task<Scenario> ResolvedScenario()
    {
        var s=await StoryRuntimeTestSupport.CreateStartedAsync();var initialized=await s.Runtime.ExecuteAsync(new InitializeNarrative(s.SessionId,8,StoryRuntimeTestSupport.Context()),default);var state=StorySession.From(s.Store.Events);
        var supplier=state.StoryletAssignments.Single(x=>x.StoryletId=="supplier-delay-response");var carrier=state.StoryletAssignments.Single(x=>x.StoryletId=="carrier-delay-response");
        var a=await s.Runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,supplier.AssignmentId,"disclose-full-delay",initialized.StateVersion,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default);
        var b=await s.Runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,carrier.AssignmentId,"reserve-emergency-capacity",a.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default);
        await s.Runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(s.SessionId,b.StateVersion,StoryRuntimeTestSupport.Context()),default);return s;
    }
}
