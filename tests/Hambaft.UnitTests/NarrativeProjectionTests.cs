using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;

namespace Hambaft.UnitTests;

public sealed class NarrativeProjectionTests
{
    [Fact] public async Task Team_views_include_only_their_private_storylet_and_localized_choices()
    {
        var(s,state)=await Initialized();var supplier=ViewProjector.Team(state,state.Teams.Single(x=>x.Id==s.SupplierTeamId),s.Package);var carrier=ViewProjector.Team(state,state.Teams.Single(x=>x.Id==s.CarrierTeamId),s.Package);
        supplier.PrivateStorylets.Should().ContainSingle(x=>x.StoryletId=="supplier-delay-response");supplier.PrivateStorylets.Should().NotContain(x=>x.StoryletId=="carrier-delay-response");supplier.PrivateStorylets.Single().Choices.Should().HaveCount(2);carrier.PrivateStorylets.Should().ContainSingle(x=>x.StoryletId=="carrier-delay-response");
    }
    [Fact] public async Task Public_projection_contains_no_private_storylet()
    {
        var(s,state)=await Initialized();var publicView=ViewProjector.Hydrate(ViewProjector.Public(state),s.Package);publicView.CurrentWorldStoryletId.Should().Be("cargo-delay-public-entry");publicView.Narrative!.StoryletId.Should().Be("cargo-delay-public-entry");Json(publicView).Should().NotContain("supplier-delay-response").And.NotContain("carrier-delay-response");
    }
    [Fact] public async Task Event_replay_produces_semantically_equal_state_and_fingerprint()
    {
        var(s,state)=await Initialized();var replay=StorySession.From(s.Store.Events);replay.Should().BeEquivalentTo(state);EventFingerprint.Compute(s.Store.Events).Should().Be(EventFingerprint.Compute(s.Store.Events.ToArray()));
    }
    private static async Task<(Scenario,StorySession)> Initialized(){var s=await StoryRuntimeTestSupport.CreateStartedAsync();await s.Runtime.ExecuteAsync(new InitializeNarrative(s.SessionId,8,StoryRuntimeTestSupport.Context()),default);return(s,StorySession.From(s.Store.Events));}
    private static string Json(object value)=>System.Text.Json.JsonSerializer.Serialize(value);
}
