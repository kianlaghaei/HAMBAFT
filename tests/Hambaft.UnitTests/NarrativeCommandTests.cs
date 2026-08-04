using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;

namespace Hambaft.UnitTests;

public sealed class NarrativeCommandTests
{
    [Fact] public async Task Initialize_assigns_entry_public_and_both_private_storylets()
    {
        var s=await StoryRuntimeTestSupport.CreateStartedAsync();var result=await Initialize(s);var state=StorySession.From(s.Store.Events);result.EventType.Should().Be(nameof(NarrativeInitialized));state.NarrativeInitialized.Should().BeTrue();state.StoryletAssignments.Should().Contain(x=>x.Scope==StoryletScope.WorldPublic).And.HaveCount(3);state.StoryletAssignments.Count(x=>x.RequiredResponse).Should().Be(2);
    }
    [Fact] public async Task Initialize_rejects_content_hash_mismatch()
    {
        var s=await StoryRuntimeTestSupport.CreateStartedAsync();s.Store.Events[0]=((SessionCreated)s.Store.Events[0]) with { ContentHash="sha256:mismatch" };await FluentActions.Invoking(()=>Initialize(s)).Should().ThrowAsync<StoryPackageHashMismatchException>();
    }
    [Fact] public async Task Choice_for_another_team_assignment_is_rejected()
    {
        var(s,version,state)=await Initialized();var assignment=state.StoryletAssignments.Single(x=>x.StoryletId=="supplier-delay-response");await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,assignment.AssignmentId,"disclose-full-delay",version,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default)).Should().ThrowAsync<DomainException>().WithMessage("*another Team*");
    }
    [Fact] public async Task Invalid_and_duplicate_choices_are_rejected()
    {
        var(s,version,state)=await Initialized();var assignment=state.StoryletAssignments.Single(x=>x.StoryletId=="supplier-delay-response");await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,assignment.AssignmentId,"invalid",version,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default)).Should().ThrowAsync<DomainException>();
        var accepted=await s.Runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,assignment.AssignmentId,"disclose-full-delay",version,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default);await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,assignment.AssignmentId,"promise-original-deadline",accepted.StateVersion,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default)).Should().ThrowAsync<DomainException>().WithMessage("*already*");
    }
    [Fact] public async Task Stale_choice_is_rejected_with_conflict()
    {
        var(s,version,state)=await Initialized();var assignment=state.StoryletAssignments.Single(x=>x.StoryletId=="supplier-delay-response");await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,assignment.AssignmentId,"disclose-full-delay",version-1,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default)).Should().ThrowAsync<ConcurrencyConflictException>();
    }
    [Fact] public async Task Early_resolution_is_refused()
    {
        var(s,version,_)=await Initialized();await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(s.SessionId,version,StoryRuntimeTestSupport.Context()),default)).Should().ThrowAsync<DomainException>().WithMessage("*missing*");
    }
    [Fact] public async Task Resolution_applies_effects_once_advances_checkpoint_and_publishes_outcome()
    {
        var(s,version,state)=await Initialized();var supplier=state.StoryletAssignments.Single(x=>x.StoryletId=="supplier-delay-response");var carrier=state.StoryletAssignments.Single(x=>x.StoryletId=="carrier-delay-response");var first=await s.Runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,supplier.AssignmentId,"disclose-full-delay",version,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default);var second=await s.Runtime.ExecuteAsync(new SubmitStoryChoice(s.SessionId,carrier.AssignmentId,"reserve-emergency-capacity",first.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default);var resolved=await s.Runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(s.SessionId,second.StateVersion,StoryRuntimeTestSupport.Context()),default);
        var final=StorySession.From(s.Store.Events);final.CurrentCheckpointId.Should().Be("outcome");final.CurrentWorldStoryletId.Should().Be("cargo-delay-coordinated-outcome");final.Metrics.Single(x=>x.Scope==MetricScope.World&&x.MetricKey=="world.pressure").NumericValue.Should().Be(48);final.StoryletAssignments.Where(x=>x.CheckpointId=="response").Should().OnlyContain(x=>x.Status==StoryletAssignmentStatus.Resolved);s.Store.Events.OfType<MetricChanged>().Should().HaveCount(4);
        await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(s.SessionId,resolved.StateVersion,StoryRuntimeTestSupport.Context()),default)).Should().ThrowAsync<DomainException>();StorySession.From(s.Store.Events).Metrics.Single(x=>x.Scope==MetricScope.World&&x.MetricKey=="world.pressure").NumericValue.Should().Be(48);
    }
    private static Task<CommandResult> Initialize(Scenario s)=>s.Runtime.ExecuteAsync(new InitializeNarrative(s.SessionId,8,StoryRuntimeTestSupport.Context()),default);
    private static async Task<(Scenario,long,StorySession)> Initialized(){var s=await StoryRuntimeTestSupport.CreateStartedAsync();var result=await Initialize(s);return(s,result.StateVersion,StorySession.From(s.Store.Events));}
}
