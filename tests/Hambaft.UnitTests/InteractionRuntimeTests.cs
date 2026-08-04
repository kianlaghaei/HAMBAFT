using System.Text.Json;
using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;

namespace Hambaft.UnitTests;

public sealed class InteractionRuntimeTests
{
    [Fact] public async Task Send_valid_proposal_and_team_views_are_private()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var sent=await Send(s,s.Version,Terms(8,1,true));var state=StorySession.From(s.Store.Events);var proposal=state.Proposals.Single();proposal.Status.Should().Be(ProposalStatus.Pending);proposal.SenderTeamId.Should().Be(s.SupplierTeamId);proposal.ReceiverTeamId.Should().Be(s.CarrierTeamId);
        var supplier=ViewProjector.Team(state,state.Teams.Single(x=>x.Id==s.SupplierTeamId),s.Package);var carrier=ViewProjector.Team(state,state.Teams.Single(x=>x.Id==s.CarrierTeamId),s.Package);supplier.Outbox.Should().ContainSingle();supplier.Inbox.Should().BeEmpty();carrier.Inbox.Should().ContainSingle();carrier.Inbox![0].AllowedActions.Should().BeEquivalentTo("Accept","Counter","Reject");sent.ResourceId.Should().NotBeEmpty();
    }
    [Fact] public async Task Invalid_receiver_and_sender_selector_are_rejected()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new SendProposal(s.SessionId,Guid.NewGuid(),"emergency-capacity-support",Guid.NewGuid(),Terms(8,1,true),null,s.Version,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default)).Should().ThrowAsync<DomainException>().WithMessage("*Receiver*");
        await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new SendProposal(s.SessionId,Guid.NewGuid(),"emergency-capacity-support",s.SupplierTeamId,Terms(8,1,true),null,s.Version,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default)).Should().ThrowAsync<DomainException>();
    }
    [Fact] public async Task Terms_schema_rejects_unknown_or_out_of_range_values()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();await FluentActions.Invoking(()=>Send(s,s.Version,Json("""{"capacityUnits":99,"supportDurationCheckpoints":1,"publicDisclosure":true}"""))).Should().ThrowAsync<DomainException>().WithMessage("*range*");
        await FluentActions.Invoking(()=>Send(s,s.Version,Json("""{"capacityUnits":8,"supportDurationCheckpoints":1,"publicDisclosure":true,"secret":1}"""))).Should().ThrowAsync<DomainException>().WithMessage("*not authored*");
    }
    [Fact] public async Task Difficulty_proposal_strictness_is_authored_and_deterministic()
    {
        var standard=await StoryRuntimeTestSupport.CreatePhase3StartedAsync("standard");await Send(standard,standard.Version,Terms(1,1,true));var hard=await StoryRuntimeTestSupport.CreatePhase3StartedAsync("hard");await FluentActions.Invoking(()=>Send(hard,hard.Version,Terms(1,1,true))).Should().ThrowAsync<DomainException>().WithMessage("*range*");
    }
    [Fact] public async Task Counter_creates_immutable_revision_and_old_terms_remain()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var sent=await Send(s,s.Version,Terms(8,1,true));var proposalId=sent.ResourceId!.Value;var counter=await s.Runtime.ExecuteAsync(new CounterProposal(s.SessionId,proposalId,1,Terms(12,2,false),sent.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default);var state=StorySession.From(s.Store.Events);
        state.Proposals.Single().CurrentRevisionNumber.Should().Be(2);state.ProposalRevisions.Should().HaveCount(2);state.ProposalRevisions[0].TermsPayload.GetProperty("capacityUnits").GetDecimal().Should().Be(8);state.ProposalRevisions[1].TermsPayload.GetProperty("capacityUnits").GetDecimal().Should().Be(12);counter.EventType.Should().Be(nameof(ProposalCountered));
    }
    [Fact] public async Task Accept_current_revision_activates_public_agreement()
    {
        var(s,proposalId,version)=await Countered();var accepted=await s.Runtime.ExecuteAsync(new AcceptProposal(s.SessionId,proposalId,2,version,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default);var state=StorySession.From(s.Store.Events);state.Proposals.Single().Status.Should().Be(ProposalStatus.Accepted);state.Agreements.Should().ContainSingle(x=>x.Status==AgreementStatus.Active&&x.AcceptedRevisionNumber==2&&x.Visibility==AgreementVisibility.Public);accepted.EmittedEventTypes.Should().ContainInOrder(nameof(ProposalAccepted),nameof(AgreementActivated));ViewProjector.Public(state).PublicAgreements.Should().ContainSingle();
    }
    [Fact] public async Task Stale_revision_acceptance_and_wrong_responder_are_rejected()
    {
        var(s,proposalId,version)=await Countered();await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new AcceptProposal(s.SessionId,proposalId,1,version,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default)).Should().ThrowAsync<DomainException>().WithMessage("*current*");await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new AcceptProposal(s.SessionId,proposalId,2,version,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default)).Should().ThrowAsync<DomainException>().WithMessage("*not allowed*");
    }
    [Fact] public async Task Reject_and_cancel_only_apply_to_current_offer_party()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var sent=await Send(s,s.Version,Terms(8,1,true));var id=sent.ResourceId!.Value;await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new CancelProposal(s.SessionId,id,1,sent.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default)).Should().ThrowAsync<DomainException>();var rejected=await s.Runtime.ExecuteAsync(new RejectProposal(s.SessionId,id,1,sent.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default);StorySession.From(s.Store.Events).Proposals.Single().Status.Should().Be(ProposalStatus.Rejected);rejected.EventType.Should().Be(nameof(ProposalRejected));
        var s2=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var sent2=await Send(s2,s2.Version,Terms(8,1,true));await s2.Runtime.ExecuteAsync(new CancelProposal(s2.SessionId,sent2.ResourceId!.Value,1,sent2.StateVersion,StoryRuntimeTestSupport.Context(s2.SupplierTeamId)),default);StorySession.From(s2.Store.Events).Proposals.Single().Status.Should().Be(ProposalStatus.Cancelled);
    }
    [Fact] public async Task Due_proposal_expires_and_cannot_be_accepted()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var sent=await Send(s,s.Version,Terms(8,1,true));var expired=await s.Runtime.ExecuteAsync(new ExpireDueProposals(s.SessionId,sent.StateVersion,StoryRuntimeTestSupport.Context()),default);StorySession.From(s.Store.Events).Proposals.Single().Status.Should().Be(ProposalStatus.Expired);expired.EventType.Should().Be(nameof(ProposalExpired));await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new AcceptProposal(s.SessionId,sent.ResourceId!.Value,1,expired.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default)).Should().ThrowAsync<DomainException>();
    }
    [Theory]
    [InlineData("accept-counter")][InlineData("accept-reject")][InlineData("cancel-accept")]
    public async Task Competing_commands_cannot_both_succeed(string race)
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var sent=await Send(s,s.Version,Terms(8,1,true));var version=sent.StateVersion;var id=sent.ResourceId!.Value;
        if(race=="cancel-accept")await s.Runtime.ExecuteAsync(new CancelProposal(s.SessionId,id,1,version,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default);else await s.Runtime.ExecuteAsync(new AcceptProposal(s.SessionId,id,1,version,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default);
        Func<Task> second=race switch{"accept-counter"=>()=>s.Runtime.ExecuteAsync(new CounterProposal(s.SessionId,id,1,Terms(9,1,false),version,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default),"accept-reject"=>()=>s.Runtime.ExecuteAsync(new RejectProposal(s.SessionId,id,1,version,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default),_=>()=>s.Runtime.ExecuteAsync(new AcceptProposal(s.SessionId,id,1,version,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default)};await FluentActions.Invoking(second).Should().ThrowAsync<ConcurrencyConflictException>();
    }
    [Fact] public async Task Two_simultaneous_counters_conflict()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var sent=await Send(s,s.Version,Terms(8,1,true));await s.Runtime.ExecuteAsync(new CounterProposal(s.SessionId,sent.ResourceId!.Value,1,Terms(9,1,false),sent.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default);await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new CounterProposal(s.SessionId,sent.ResourceId.Value,1,Terms(10,1,false),sent.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default)).Should().ThrowAsync<ConcurrencyConflictException>();
    }
    [Fact] public async Task Simultaneous_expire_and_accept_conflict()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var sent=await Send(s,s.Version,Terms(8,1,true));var version=sent.StateVersion;await s.Runtime.ExecuteAsync(new ExpireDueProposals(s.SessionId,version,StoryRuntimeTestSupport.Context()),default);await FluentActions.Invoking(()=>s.Runtime.ExecuteAsync(new AcceptProposal(s.SessionId,sent.ResourceId!.Value,1,version,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default)).Should().ThrowAsync<ConcurrencyConflictException>();
    }
    [Fact] public async Task Agreement_failure_is_meaningful_and_terminal()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var sent=await Send(s,s.Version,Terms(8,1,true));var accepted=await s.Runtime.ExecuteAsync(new AcceptProposal(s.SessionId,sent.ResourceId!.Value,1,sent.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default);var failed=await s.Runtime.ExecuteAsync(new FailAgreement(s.SessionId,accepted.ResourceId!.Value,"authored-failure",accepted.StateVersion,StoryRuntimeTestSupport.Context()),default);var state=StorySession.From(s.Store.Events);state.Agreements.Single().Status.Should().Be(AgreementStatus.Failed);state.Proposals.Single().Status.Should().Be(ProposalStatus.Failed);failed.EventType.Should().Be(nameof(AgreementFailed));
    }
    [Fact] public async Task Immediate_agreement_executes_in_acceptance_batch()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var interaction=s.Package.InteractionDefinitions.Single() with { ExecutionMode=InteractionExecutionMode.ImmediateOnAcceptance };var runtime=RuntimeFor(s,s.Package with { Interactions=[interaction] });var sent=await runtime.ExecuteAsync(new SendProposal(s.SessionId,Guid.NewGuid(),interaction.Id,s.CarrierTeamId,Terms(8,1,true),null,s.Version,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default);var accepted=await runtime.ExecuteAsync(new AcceptProposal(s.SessionId,sent.ResourceId!.Value,1,sent.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default);StorySession.From(s.Store.Events).Agreements.Single().Status.Should().Be(AgreementStatus.Executed);accepted.EmittedEventTypes.Should().Contain(nameof(AgreementExecuted));
    }
    [Fact] public async Task Manual_agreement_waits_for_party_execution_command()
    {
        var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var interaction=s.Package.InteractionDefinitions.Single() with { ExecutionMode=InteractionExecutionMode.ManualExecution };var runtime=RuntimeFor(s,s.Package with { Interactions=[interaction] });var sent=await runtime.ExecuteAsync(new SendProposal(s.SessionId,Guid.NewGuid(),interaction.Id,s.CarrierTeamId,Terms(8,1,true),null,s.Version,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default);var accepted=await runtime.ExecuteAsync(new AcceptProposal(s.SessionId,sent.ResourceId!.Value,1,sent.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default);StorySession.From(s.Store.Events).Agreements.Single().Status.Should().Be(AgreementStatus.Active);await runtime.ExecuteAsync(new ExecuteAgreement(s.SessionId,accepted.ResourceId!.Value,accepted.StateVersion,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default);StorySession.From(s.Store.Events).Agreements.Single().Status.Should().Be(AgreementStatus.Executed);
    }
    [Fact] public void Private_agreement_is_visible_only_to_parties_and_never_public()
    {
        var session=Guid.NewGuid();var a=Guid.NewGuid();var b=Guid.NewGuid();var third=Guid.NewGuid();var proposal=Guid.NewGuid();var agreement=Guid.NewGuid();var metadata=StoryRuntimeTestSupport.Context().For(session) with { CheckpointId="response" };var terms=Terms(8,1,true);var events=new IDomainEvent[]{new SessionCreated(session,"sample","1.0.0","sha256:test",1,metadata),new TeamAdded(a,"A","hash",metadata with { TeamId=a }),new TeamAdded(b,"B","hash",metadata with { TeamId=b }),new TeamAdded(third,"Third","hash",metadata with { TeamId=third }),new NarrativeInitialized("sample","1.0.0","sha256:test","response",metadata),new ProposalSent(proposal,"support",a,b,1,terms,[],[],"response","response",6,metadata with { TeamId=a }),new ProposalAccepted(proposal,"support",a,b,1,agreement,metadata with { TeamId=b }),new AgreementActivated(agreement,proposal,1,"support",[a,b],terms,"response",AgreementVisibility.PrivateToParties,metadata)};var state=StorySession.From(events);ViewProjector.Team(state,state.Teams.Single(x=>x.Id==a)).ActiveAgreements.Should().ContainSingle();ViewProjector.Team(state,state.Teams.Single(x=>x.Id==b)).ActiveAgreements.Should().ContainSingle();ViewProjector.Team(state,state.Teams.Single(x=>x.Id==third)).ActiveAgreements.Should().BeEmpty();var publicView=ViewProjector.Public(state);publicView.PublicAgreements.Should().BeEmpty();JsonSerializer.Serialize(publicView).Should().NotContain(proposal.ToString("D"));
    }

    private static async Task<(Phase3Scenario Scenario,Guid ProposalId,long Version)> Countered(){var s=await StoryRuntimeTestSupport.CreatePhase3StartedAsync();var sent=await Send(s,s.Version,Terms(8,1,true));var counter=await s.Runtime.ExecuteAsync(new CounterProposal(s.SessionId,sent.ResourceId!.Value,1,Terms(12,2,false),sent.StateVersion,StoryRuntimeTestSupport.Context(s.CarrierTeamId)),default);return(s,sent.ResourceId.Value,counter.StateVersion);}
    private static Task<CommandResult> Send(Phase3Scenario s,long version,JsonElement terms)=>s.Runtime.ExecuteAsync(new SendProposal(s.SessionId,Guid.NewGuid(),"emergency-capacity-support",s.CarrierTeamId,terms,null,version,StoryRuntimeTestSupport.Context(s.SupplierTeamId)),default);
    private static SessionRuntime RuntimeFor(Phase3Scenario s,StoryPackage package)=>new(s.Store,new FixedTestPairing(),new FixedPackageLoader(package),new DeterministicStoryletSelector(new ConditionEngine()),new EffectEngine(),new DeterministicBehaviorResolver(new ConditionEngine()));
    internal static JsonElement Terms(decimal capacity,decimal duration,bool disclosure)=>Json($$"""{"capacityUnits":{{capacity}},"supportDurationCheckpoints":{{duration}},"publicDisclosure":{{disclosure.ToString().ToLowerInvariant()}}}""");
    internal static JsonElement Json(string json)=>JsonDocument.Parse(json).RootElement.Clone();
}
