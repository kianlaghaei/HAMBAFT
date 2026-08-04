using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;
using System.Text.Json;

namespace Hambaft.UnitTests;

public sealed class BehaviorAndConsequenceTests
{
    [Fact] public async Task Eligible_priority_and_weighted_rule_selection_is_deterministic()
    {
        var first=await StoryRuntimeTestSupport.CreatePhase3StartedAsync(seed:42);var selected1=await first.Runtime.ExecuteAsync(new ResolveUncontrolledEntityActions(first.SessionId,first.Version,StoryRuntimeTestSupport.Context()),default);var action1=StorySession.From(first.Store.Events).AuthoredBehaviorSelections.Single();
        var second=await StoryRuntimeTestSupport.CreatePhase3StartedAsync(seed:42);await second.Runtime.ExecuteAsync(new ResolveUncontrolledEntityActions(second.SessionId,second.Version,StoryRuntimeTestSupport.Context()),default);var action2=StorySession.From(second.Store.Events).AuthoredBehaviorSelections.Single();
        action2.Should().BeEquivalentTo(action1);selected1.EventType.Should().Be(nameof(AuthoredBehaviorActionSelected));
    }
    [Fact] public async Task Different_valid_seeds_can_select_different_weighted_actions()
    {
        var actions=new HashSet<string>(StringComparer.Ordinal);for(var seed=1;seed<=30&&actions.Count<2;seed++){var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync(seed:seed);await s.Runtime.ExecuteAsync(new ResolveUncontrolledEntityActions(s.SessionId,s.Version,StoryRuntimeTestSupport.Context()),default);actions.Add(StorySession.From(s.Store.Events).AuthoredBehaviorSelections.Single().ActionId);}actions.Should().HaveCountGreaterThan(1);
    }
    [Fact] public async Task Rule_and_entity_collection_order_do_not_change_selection_or_stable_entity_order()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var state=StorySession.From(s.Store.Events);var extra=Guid.Parse("31000000-0000-0000-0000-000000000000");state.Apply(new WorldEntityCreated(extra,"credit-provider","Second Credit",ControllerType.AuthoredBehavior,"cooperative-credit",EntityStatus.Active,StoryRuntimeTestSupport.Context().For(s.SessionId)));var resolver=new DeterministicBehaviorResolver(new ConditionEngine());var original=resolver.Resolve(state,s.Package);original.Select(x=>x.EntityId).Should().BeInAscendingOrder();state.Entities.Reverse();var profile=s.Package.BehaviorDefinitions.Profiles.Single(x=>x.Id=="cooperative-credit");var reversedPackage=s.Package with { Behaviors=s.Package.BehaviorDefinitions with { Profiles=s.Package.BehaviorDefinitions.Profiles.Select(x=>x.Id==profile.Id?x with { Rules=x.Rules.Reverse().ToList() }:x).Reverse().ToList(),Actions=s.Package.BehaviorDefinitions.Actions.Reverse().ToList() } };var reordered=resolver.Resolve(state,reversedPackage);reordered.Should().BeEquivalentTo(original,options=>options.WithStrictOrdering());
    }
    [Fact] public async Task Difficulty_modifiers_change_authored_weighting_predictably()
    {
        var standard=await ActionFor("standard",42);var hard=await ActionFor("hard",42);standard.Should().NotBe(hard);
        var standardOpportunistic=await OpportunisticSelections("standard");var hardOpportunistic=await OpportunisticSelections("hard");hardOpportunistic.Should().BeGreaterThan(standardOpportunistic);
    }
    [Fact] public async Task Fallback_action_is_used_when_no_rule_is_eligible()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var state=StorySession.From(s.Store.Events);var profile=s.Package.BehaviorDefinitions.Profiles.Single(x=>x.Id=="cooperative-credit");var impossible=new ConditionDefinition(ConditionType.MetricAbove,ConditionTargetScope.World,"world.pressure",101);var package=s.Package with { Behaviors=s.Package.BehaviorDefinitions with { Profiles=s.Package.BehaviorDefinitions.Profiles.Select(x=>x.Id==profile.Id?x with { Rules=x.Rules.Select(r=>r with { Conditions=[impossible] }).ToList() }:x).ToList() } };new DeterministicBehaviorResolver(new ConditionEngine()).Resolve(state,package).Single().Action.Id.Should().Be("tighten-credit");
    }
    [Fact] public async Task Once_per_checkpoint_rule_is_not_selected_twice_at_same_checkpoint()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var first=await s.Runtime.ExecuteAsync(new ResolveUncontrolledEntityActions(s.SessionId,s.Version,StoryRuntimeTestSupport.Context()),default);var firstSelection=StorySession.From(s.Store.Events).AuthoredBehaviorSelections.Single();await s.Runtime.ExecuteAsync(new ResolveUncontrolledEntityActions(s.SessionId,first.StateVersion,StoryRuntimeTestSupport.Context()),default);var selections=StorySession.From(s.Store.Events).AuthoredBehaviorSelections;selections.Should().HaveCount(2);selections[1].RuleId.Should().NotBe(firstSelection.RuleId);
    }
    [Fact] public async Task Promise_choice_schedules_next_checkpoint_consequence_and_it_triggers_once()
    {
        var flow=await RunFlow("promise-original-deadline",withAgreement:true,withExpiringProposal:true);var afterFirst=flow.AfterFirst;afterFirst.ScheduledConsequences.Should().ContainSingle(x=>x.DefinitionId=="deadline-promise-review"&&x.Status==ScheduledConsequenceStatus.Pending&&x.DueCheckpointId=="outcome");
        var supplierReliabilityBefore=afterFirst.Metrics.Single(x=>x.Scope==MetricScope.Entity&&x.ScopeId==flow.Scenario.SupplierEntityId&&x.MetricKey=="entity.reliability").NumericValue;var second=await flow.Scenario.Runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(flow.Scenario.SessionId,flow.Version,StoryRuntimeTestSupport.Context()),default);var final=StorySession.From(flow.Scenario.Store.Events);final.ScheduledConsequences.Single().Status.Should().Be(ScheduledConsequenceStatus.Triggered);final.Metrics.Single(x=>x.Scope==MetricScope.Entity&&x.ScopeId==flow.Scenario.SupplierEntityId&&x.MetricKey=="entity.reliability").NumericValue.Should().Be(supplierReliabilityBefore-4);final.CurrentCheckpointId.Should().Be("coordination-result");flow.Scenario.Store.Events.OfType<ConsequenceTriggered>().Should().ContainSingle();await FluentActions.Invoking(()=>flow.Scenario.Runtime.ExecuteAsync(new ResolveDueConsequences(flow.Scenario.SessionId,second.StateVersion,StoryRuntimeTestSupport.Context()),default)).Should().NotThrowAsync();flow.Scenario.Store.Events.OfType<ConsequenceTriggered>().Should().ContainSingle();
    }
    [Fact] public async Task Disclosure_does_not_schedule_deadline_penalty()
    {
        var flow=await RunFlow("disclose-full-delay",withAgreement:false,withExpiringProposal:false);flow.AfterFirst.ScheduledConsequences.Should().BeEmpty();
    }
    [Fact] public async Task Pending_consequence_can_be_cancelled_and_does_not_trigger()
    {
        var flow=await RunFlow("promise-original-deadline",withAgreement:false,withExpiringProposal:false);var pending=flow.AfterFirst.ScheduledConsequences.Single();var cancelled=await flow.Scenario.Runtime.ExecuteAsync(new CancelConsequence(flow.Scenario.SessionId,pending.ScheduledConsequenceId,flow.Version,StoryRuntimeTestSupport.Context()),default);await flow.Scenario.Runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(flow.Scenario.SessionId,cancelled.StateVersion,StoryRuntimeTestSupport.Context()),default);var state=StorySession.From(flow.Scenario.Store.Events);state.ScheduledConsequences.Single().Status.Should().Be(ScheduledConsequenceStatus.Cancelled);flow.Scenario.Store.Events.OfType<ConsequenceTriggered>().Should().BeEmpty();
    }
    [Fact] public async Task At_checkpoint_trigger_resolves_deterministically()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var definition=new ConsequenceDefinition("at-response",new(ConsequenceTriggerType.AtCheckpoint,CheckpointId:"response"),[],[],null,ConsequenceVisibility.SystemOnly,RepeatPolicy.OncePerSession);var runtime=RuntimeFor(s,s.Package with { Consequences=[..s.Package.ConsequenceDefinitions,definition] });var scheduled=await runtime.ExecuteAsync(new ScheduleConsequence(s.SessionId,definition.Id,Guid.NewGuid(),s.SupplierTeamId,s.SupplierEntityId,s.Version,StoryRuntimeTestSupport.Context()),default);await runtime.ExecuteAsync(new ResolveDueConsequences(s.SessionId,scheduled.StateVersion,StoryRuntimeTestSupport.Context()),default);StorySession.From(s.Store.Events).ScheduledConsequences.Single(x=>x.DefinitionId==definition.Id).Status.Should().Be(ScheduledConsequenceStatus.Triggered);
    }
    [Fact] public async Task Agreement_execution_trigger_resolves_from_event_history()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var definition=new ConsequenceDefinition("after-agreement",new(ConsequenceTriggerType.WhenAgreementExecuted,AgreementInteractionTypeId:"emergency-capacity-support"),[],[],null,ConsequenceVisibility.SystemOnly,RepeatPolicy.OncePerSession);var package=s.Package with { Consequences=[..s.Package.ConsequenceDefinitions,definition] };var state=StorySession.From(s.Store.Events);var proposal=Guid.NewGuid();var agreement=Guid.NewGuid();var metadata=StoryRuntimeTestSupport.Context().For(s.SessionId) with { CheckpointId=state.CurrentCheckpointId };s.Store.Events.Add(new ProposalSent(proposal,"emergency-capacity-support",s.SupplierTeamId,s.CarrierTeamId,1,InteractionRuntimeTests.Terms(8,1,true),[],[],"response","response",state.StateVersion+1,metadata));s.Store.Events.Add(new ProposalAccepted(proposal,"emergency-capacity-support",s.SupplierTeamId,s.CarrierTeamId,1,agreement,metadata));s.Store.Events.Add(new AgreementActivated(agreement,proposal,1,"emergency-capacity-support",[s.SupplierTeamId,s.CarrierTeamId],InteractionRuntimeTests.Terms(8,1,true),"response",AgreementVisibility.PrivateToParties,metadata));s.Store.Events.Add(new AgreementExecuted(agreement,proposal,"emergency-capacity-support",[s.SupplierTeamId,s.CarrierTeamId],1,"response",metadata));var runtime=RuntimeFor(s,package);var scheduled=await runtime.ExecuteAsync(new ScheduleConsequence(s.SessionId,definition.Id,proposal,s.SupplierTeamId,s.SupplierEntityId,s.Store.Events.Count,StoryRuntimeTestSupport.Context()),default);await runtime.ExecuteAsync(new ResolveDueConsequences(s.SessionId,scheduled.StateVersion,StoryRuntimeTestSupport.Context()),default);StorySession.From(s.Store.Events).ScheduledConsequences.Single(x=>x.DefinitionId==definition.Id).Status.Should().Be(ScheduledConsequenceStatus.Triggered);
    }
    [Fact] public async Task Memory_exists_trigger_resolves_at_resolution()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var definition=new ConsequenceDefinition("when-memory",new(ConsequenceTriggerType.WhenMemoryExistsAtResolution,MemoryKey:"marker",MemoryScope:ConditionTargetScope.CurrentEntity),[],[],null,ConsequenceVisibility.SystemOnly,RepeatPolicy.OncePerSession);var package=s.Package with { Consequences=[..s.Package.ConsequenceDefinitions,definition] };var state=StorySession.From(s.Store.Events);s.Store.Events.Add(new StoryMemoryAdded(MemoryScope.Entity,s.SupplierEntityId,"marker",null,MemoryVisibility.EntityPrivate,"$test",StoryRuntimeTestSupport.Context().For(s.SessionId) with { TeamId=s.SupplierTeamId,CheckpointId=state.CurrentCheckpointId }));var runtime=RuntimeFor(s,package);var scheduled=await runtime.ExecuteAsync(new ScheduleConsequence(s.SessionId,definition.Id,Guid.NewGuid(),s.SupplierTeamId,s.SupplierEntityId,s.Store.Events.Count,StoryRuntimeTestSupport.Context()),default);await runtime.ExecuteAsync(new ResolveDueConsequences(s.SessionId,scheduled.StateVersion,StoryRuntimeTestSupport.Context()),default);StorySession.From(s.Store.Events).ScheduledConsequences.Single(x=>x.DefinitionId==definition.Id).Status.Should().Be(ScheduledConsequenceStatus.Triggered);
    }
    [Fact] public async Task Private_consequence_isolated_while_public_consequence_is_projected()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var state=StorySession.From(s.Store.Events);var metadata=StoryRuntimeTestSupport.Context(s.SupplierTeamId).For(s.SessionId) with { CheckpointId=state.CurrentCheckpointId };var privateEvent=new ConsequenceScheduled(Guid.NewGuid(),"private-test",Guid.NewGuid(),s.SupplierTeamId,s.SupplierEntityId,state.CurrentCheckpointId!,"outcome",ConsequenceTriggerType.AtCheckpoint,ConsequenceVisibility.PrivateToSourceTeam,metadata);state.Apply(privateEvent);var supplier=ViewProjector.Team(state,state.Teams.Single(x=>x.Id==s.SupplierTeamId),s.Package);var carrier=ViewProjector.Team(state,state.Teams.Single(x=>x.Id==s.CarrierTeamId),s.Package);supplier.PendingConsequences.Should().ContainSingle();carrier.PendingConsequences.Should().BeEmpty();ViewProjector.Public(state).PublicConsequences.Should().BeEmpty();
        var publicEvent=privateEvent with { ScheduledConsequenceId=Guid.NewGuid(),DefinitionId="public-test",Visibility=ConsequenceVisibility.Public };state.Apply(publicEvent);ViewProjector.Public(state).PublicConsequences.Should().ContainSingle(x=>x.DefinitionId=="public-test");
    }
    [Fact] public async Task Agreement_executes_before_behavior_and_team_choice_effects()
    {
        var flow=await RunFlow("promise-original-deadline",withAgreement:true,withExpiringProposal:true);var events=flow.Scenario.Store.Events;events.IndexOf(events.OfType<AgreementExecuted>().Single()).Should().BeLessThan(events.IndexOf(events.OfType<AuthoredBehaviorActionSelected>().First()));events.IndexOf(events.OfType<AuthoredBehaviorActionSelected>().First()).Should().BeLessThan(events.IndexOf(events.OfType<StoryMemoryAdded>().Single(x=>x.Key=="supplier_promised_original_deadline")));flow.AfterFirst.Agreements.Single().Status.Should().Be(AgreementStatus.Executed);flow.AfterFirst.Proposals.Should().Contain(x=>x.Status==ProposalStatus.Expired);
    }
    [Fact] public async Task Checkpoint_failure_appends_no_partial_phase3_events_or_effects()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var broken=s.Package with { Effects=s.Package.Effects.Where(x=>x.Id!="agreement-carrier-capacity").ToList() };var runtime=RuntimeFor(s,broken);var sent=await runtime.ExecuteAsync(new SendProposal(s.SessionId,Guid.NewGuid(),"emergency-capacity-support",s.CarrierTeamId,InteractionRuntimeTests.Terms(8,1,true),null,s.Version,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default);var accepted=await runtime.ExecuteAsync(new AcceptProposal(s.SessionId,sent.ResourceId!.Value,1,sent.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default);var state=StorySession.From(s.Store.Events);var supplier=state.StoryletAssignments.Single(x=>x.StoryletId=="supplier-delay-response");var carrier=state.StoryletAssignments.Single(x=>x.StoryletId=="carrier-delay-response");var first=await runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,supplier.AssignmentId,"promise-original-deadline",accepted.StateVersion,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default);var second=await runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,carrier.AssignmentId,"reserve-emergency-capacity",first.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default);var count=s.Store.Events.Count;await FluentActions.Invoking(()=>runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(s.SessionId,second.StateVersion,StoryRuntimeTestSupport.Context()),default)).Should().ThrowAsync<InvalidOperationException>();s.Store.Events.Should().HaveCount(count);state=StorySession.From(s.Store.Events);state.Agreements.Single().Status.Should().Be(AgreementStatus.Active);state.AuthoredBehaviorSelections.Should().BeEmpty();state.CurrentCheckpointId.Should().Be("response");
    }
    [Fact] public async Task Replay_preserves_behavior_consequence_and_fingerprint()
    {
        var first=await CompleteFlow();var second=await CompleteFlow();JsonSerializer.Serialize(StorySession.From(first.Events)).Should().Be(JsonSerializer.Serialize(first.State));JsonSerializer.Serialize(first.State).Should().Be(JsonSerializer.Serialize(second.State));first.Fingerprint.Should().Be(second.Fingerprint);
    }

    private static async Task<string> ActionFor(string difficulty,int seed){var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync(difficulty,seed);await s.Runtime.ExecuteAsync(new ResolveUncontrolledEntityActions(s.SessionId,s.Version,StoryRuntimeTestSupport.Context()),default);return StorySession.From(s.Store.Events).AuthoredBehaviorSelections.Single().ActionId;}
    private static async Task<int> OpportunisticSelections(string difficulty){var count=0;for(var seed=1;seed<=40;seed++)if(await ActionFor(difficulty,seed)=="offer-costly-credit")count++;return count;}
    private static async Task<FlowResult> RunFlow(string supplierChoice,bool withAgreement,bool withExpiringProposal)
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var version=s.Version;var sequence=0;CommandContext Next(Guid? team=null)=>StoryRuntimeTestSupport.Context(team,DeterministicIds.Create("phase3-flow",++sequence));
        if(withAgreement){var sent=await s.Runtime.ExecuteAsync(new SendProposal(s.SessionId,DeterministicIds.Create("phase3-proposal","accepted"),"emergency-capacity-support",s.CarrierTeamId,InteractionRuntimeTests.Terms(8,1,true),null,version,Next(s.SupplierTeamId)),default);var proposalId=sent.ResourceId!.Value;var counter=await s.Runtime.ExecuteAsync(new CounterProposal(s.SessionId,proposalId,1,InteractionRuntimeTests.Terms(12,2,false),sent.StateVersion,Next(s.CarrierTeamId)),default);var accepted=await s.Runtime.ExecuteAsync(new AcceptProposal(s.SessionId,proposalId,2,counter.StateVersion,Next(s.SupplierTeamId)),default);version=accepted.StateVersion;}
        if(withExpiringProposal){var unanswered=await s.Runtime.ExecuteAsync(new SendProposal(s.SessionId,DeterministicIds.Create("phase3-proposal","unanswered"),"emergency-capacity-support",s.CarrierTeamId,InteractionRuntimeTests.Terms(4,1,true),null,version,Next(s.SupplierTeamId)),default);version=unanswered.StateVersion;}
        var state=StorySession.From(s.Store.Events);var supplier=state.StoryletAssignments.Single(x=>x.StoryletId=="supplier-delay-response");var carrier=state.StoryletAssignments.Single(x=>x.StoryletId=="carrier-delay-response");var first=await s.Runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,supplier.AssignmentId,supplierChoice,version,Next(s.SupplierTeamId)),default);var second=await s.Runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,carrier.AssignmentId,"reserve-emergency-capacity",first.StateVersion,Next(s.CarrierTeamId)),default);var resolved=await s.Runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(s.SessionId,second.StateVersion,Next()),default);return new(s,StorySession.From(s.Store.Events),resolved.StateVersion);
    }
    private static Task<CommandResult> Send(Phase3Scenario s,long version,int units)=>s.Runtime.ExecuteAsync(new SendProposal(s.SessionId,Guid.NewGuid(),"emergency-capacity-support",s.CarrierTeamId,InteractionRuntimeTests.Terms(units,1,true),null,version,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default);
    private static SessionRuntime RuntimeFor(Phase3Scenario s,StoryPackage package)=>new(s.Store,new FixedTestPairing(),new FixedPackageLoader(package),new DeterministicStoryletSelector(new ConditionEngine()),new EffectEngine(),new DeterministicBehaviorResolver(new ConditionEngine()));
    private static async Task<(StorySession State,IReadOnlyList<IDomainEvent> Events,string Fingerprint)> CompleteFlow(){var flow=await RunFlow("promise-original-deadline",true,true);await flow.Scenario.Runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(flow.Scenario.SessionId,flow.Version,StoryRuntimeTestSupport.Context(command:DeterministicIds.Create("phase3-flow","second-resolution"))),default);var state=StorySession.From(flow.Scenario.Store.Events);return(state,flow.Scenario.Store.Events.ToList(),EventFingerprint.Compute(flow.Scenario.Store.Events));}
    private sealed record FlowResult(Phase3Scenario Scenario,StorySession AfterFirst,long Version);
}

internal sealed class FixedPackageLoader(StoryPackage package) : IStoryPackageLoader
{
    public Task<StoryPackage> LoadAsync(string packageId,string version,CancellationToken cancellationToken)=>Task.FromResult(package);
}

internal static class EventListExtensions
{
    public static int IndexOf(this IReadOnlyList<IDomainEvent> values,IDomainEvent value){for(var i=0;i<values.Count;i++)if(ReferenceEquals(values[i],value))return i;return -1;}
}
