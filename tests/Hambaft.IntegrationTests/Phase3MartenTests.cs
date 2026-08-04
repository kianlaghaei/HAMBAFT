using System.Text.Json;
using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;
using Hambaft.Infrastructure.Persistence;
using Hambaft.Infrastructure.Security;
using Hambaft.Infrastructure.StoryPackages;

namespace Hambaft.IntegrationTests;

[Collection("postgres")]
public sealed class Phase3MartenTests(PostgresFixture fixture)
{
    [PostgresFact]
    public async Task Proposal_agreement_behavior_and_consequence_persist_rebuild_and_replay_semantically()
    {
        var loader=Packages();var package=await loader.LoadAsync("sample-cargo-delay","1.1.0",default);var sessionId=Guid.NewGuid();var supplierTeam=Guid.NewGuid();var carrierTeam=Guid.NewGuid();var supplier=Guid.NewGuid();var carrier=Guid.NewGuid();var credit=Guid.NewGuid();var contexts=new Contexts();long finalVersion;string fingerprint;
        await using(var document=fixture.Store!.LightweightSession())
        {
            var store=new MartenSessionStore(document,loader);var runtime=Runtime(store,loader);
            await runtime.ExecuteAsync(new CreateSession(sessionId,package.Manifest.Id,package.Manifest.Version,package.ContentHash,42,0,contexts.Next(),"standard"),default);await runtime.ExecuteAsync(new AddTeam(sessionId,supplierTeam,"Supplier",1,contexts.Next()),default);await runtime.ExecuteAsync(new AddTeam(sessionId,carrierTeam,"Carrier",2,contexts.Next()),default);await runtime.ExecuteAsync(new CreateWorldEntity(sessionId,supplier,"supplier","Supplier",ControllerType.HumanTeam,null,3,contexts.Next()),default);await runtime.ExecuteAsync(new CreateWorldEntity(sessionId,carrier,"carrier","Carrier",ControllerType.HumanTeam,null,4,contexts.Next()),default);await runtime.ExecuteAsync(new CreateWorldEntity(sessionId,credit,"credit-provider","Credit",ControllerType.AuthoredBehavior,"cooperative-credit",5,contexts.Next()),default);await runtime.ExecuteAsync(new AssignEntityToTeam(sessionId,supplier,supplierTeam,6,contexts.Next()),default);await runtime.ExecuteAsync(new AssignEntityToTeam(sessionId,carrier,carrierTeam,7,contexts.Next()),default);await runtime.ExecuteAsync(new StartSession(sessionId,8,contexts.Next()),default);var initialized=await runtime.ExecuteAsync(new InitializeNarrative(sessionId,9,contexts.Next()),default);
            var state=(await store.LoadAsync(sessionId,default))!;var supplierAssignment=state.StoryletAssignments.Single(x=>x.StoryletId=="supplier-delay-response");var carrierAssignment=state.StoryletAssignments.Single(x=>x.StoryletId=="carrier-delay-response");var choice1=await runtime.ExecuteAsync(new SubmitStoryChoice(sessionId,supplierAssignment.AssignmentId,"promise-original-deadline",initialized.StateVersion,contexts.Next(supplierTeam)),default);var choice2=await runtime.ExecuteAsync(new SubmitStoryChoice(sessionId,carrierAssignment.AssignmentId,"reserve-emergency-capacity",choice1.StateVersion,contexts.Next(carrierTeam)),default);
            var proposalId=Guid.NewGuid();var sent=await runtime.ExecuteAsync(new SendProposal(sessionId,proposalId,"emergency-capacity-support",carrierTeam,Terms(8),null,choice2.StateVersion,contexts.Next(supplierTeam)),default);var countered=await runtime.ExecuteAsync(new CounterProposal(sessionId,proposalId,1,Terms(12),sent.StateVersion,contexts.Next(carrierTeam)),default);var accepted=await runtime.ExecuteAsync(new AcceptProposal(sessionId,proposalId,2,countered.StateVersion,contexts.Next(supplierTeam)),default);var unanswered=await runtime.ExecuteAsync(new SendProposal(sessionId,Guid.NewGuid(),"emergency-capacity-support",carrierTeam,Terms(3),null,accepted.StateVersion,contexts.Next(supplierTeam)),default);var first=await runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(sessionId,unanswered.StateVersion,contexts.Next()),default);var second=await runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(sessionId,first.StateVersion,contexts.Next()),default);finalVersion=second.StateVersion;var events=await store.LoadEventsAsync(sessionId,default);fingerprint=EventFingerprint.Compute(events);
        }
        await using(var query=fixture.Store!.LightweightSession())
        {
            var store=new MartenSessionStore(query,loader);var events=await store.LoadEventsAsync(sessionId,default);var aggregate=StorySession.From(events);aggregate.StateVersion.Should().Be(finalVersion);aggregate.ProposalRevisions.Should().HaveCount(3);aggregate.Proposals.Should().Contain(x=>x.Status==ProposalStatus.Expired);aggregate.Agreements.Should().ContainSingle(x=>x.Status==AgreementStatus.Executed);aggregate.AuthoredBehaviorSelections.Should().HaveCount(2);aggregate.ScheduledConsequences.Should().ContainSingle(x=>x.Status==ScheduledConsequenceStatus.Triggered);EventFingerprint.Compute(events).Should().Be(fingerprint);
            var supplierView=await store.LoadTeamViewAsync(supplierTeam,default);var carrierView=await store.LoadTeamViewAsync(carrierTeam,default);supplierView!.Agreements.Should().ContainSingle();carrierView!.Agreements.Should().ContainSingle();var publicView=await store.LoadPublicViewAsync(sessionId,default);publicView!.PublicAgreements.Should().ContainSingle();JsonSerializer.Serialize(publicView).Should().NotContain("pairingCodeHash");
        }
        using(var daemon=await fixture.Store!.BuildProjectionDaemonAsync()){await daemon.RebuildProjectionAsync("SessionExperience",default);await daemon.RebuildProjectionAsync("SessionState",default);await daemon.RebuildProjectionAsync("PublicWorld",default);}
        await using(var rebuilt=fixture.Store.QuerySession()){var state=await rebuilt.LoadAsync<SessionStateView>(sessionId);state!.StateVersion.Should().Be(finalVersion);state.Proposals.Should().HaveCount(2);state.Agreements.Should().ContainSingle(x=>x.Status==AgreementStatus.Executed);state.ScheduledConsequences.Should().ContainSingle(x=>x.Status==ScheduledConsequenceStatus.Triggered);}
    }

    private static SessionRuntime Runtime(ISessionStore store,IStoryPackageLoader loader)=>new(store,new PairingCodeGenerator(),loader,new DeterministicStoryletSelector(new ConditionEngine()),new EffectEngine(),new DeterministicBehaviorResolver(new ConditionEngine()));
    private static IStoryPackageLoader Packages(){var root=FindStories();var hasher=new DeterministicStoryPackageHasher();return new FileSystemStoryPackageLoader(new StoryPackageOptions{Root=root},hasher,new StoryPackageValidator());}
    private static string FindStories(){var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!Directory.Exists(Path.Combine(directory.FullName,"stories")))directory=directory.Parent;return Path.Combine(directory!.FullName,"stories");}
    private static JsonElement Terms(decimal units)=>JsonDocument.Parse($$"""{"capacityUnits":{{units}},"supportDurationCheckpoints":1,"publicDisclosure":true}""").RootElement.Clone();
    private sealed class Contexts{private int value;public CommandContext Next(Guid? team=null){var id=DeterministicIds.Create("phase3-integration",++value);return new(id,id,id,team,new DateTimeOffset(2026,8,4,8,0,0,TimeSpan.Zero));}}
}
