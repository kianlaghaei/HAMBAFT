using FluentAssertions;
using Hambaft.Application;
using Hambaft.Domain;
using Hambaft.Infrastructure.Persistence;

namespace Hambaft.UnitTests;

public sealed class ProjectionIsolationTests
{
    [Fact]
    public void Public_inline_projection_never_accepts_private_memory()
    {
        var sessionId=Guid.NewGuid();var teamId=Guid.NewGuid();var now=DateTimeOffset.UtcNow;var metadata=new EventMetadata(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),sessionId,null,now);var projection=new PublicWorldProjection();
        var view=projection.Create(new SessionCreated(sessionId,"pkg","1","hash",1,metadata));
        view=projection.Apply(new InitialMemoryAdded(MemoryScope.Team,teamId,"secret","{}",MemoryVisibility.TeamPrivate,metadata with { TeamId=teamId }),view);
        view=projection.Apply(new InitialMemoryAdded(MemoryScope.World,sessionId,"public",null,MemoryVisibility.Public,metadata),view);
        view.PublicMemories.Should().ContainSingle(x=>x.Key=="public");view.PublicMemories.Should().NotContain(x=>x.Key=="secret");view.StateVersion.Should().Be(3);
    }

    [Fact]
    public void Team_projection_contains_only_its_own_private_memory()
    {
        var sessionId=Guid.NewGuid();var first=Guid.NewGuid();var second=Guid.NewGuid();var now=DateTimeOffset.UtcNow;EventMetadata Meta(Guid? team=null)=>new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),sessionId,team,now);
        var created=StorySession.Create(sessionId,"pkg","1","hash",1,Meta());var state=StorySession.From([created]);var e1=state.AddTeam(first,"First","hash",Meta(first));state.Apply(e1);var e2=state.AddTeam(second,"Second","hash",Meta(second));state.Apply(e2);var own=state.AddInitialMemory(MemoryScope.Team,first,"own",null,MemoryVisibility.TeamPrivate,Meta(first));state.Apply(own);var other=state.AddInitialMemory(MemoryScope.Team,second,"other",null,MemoryVisibility.TeamPrivate,Meta(second));state.Apply(other);
        ViewProjector.Team(state,state.Teams.Single(x=>x.Id==first)).VisibleMemories.Select(x=>x.Key).Should().Equal("own");
    }
}
