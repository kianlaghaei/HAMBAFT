using FluentAssertions;
using Hambaft.Domain;

namespace Hambaft.EndToEndTests;

public sealed class DeterministicScenarioTests
{
    [Fact]
    public void Two_team_scenario_replays_to_identical_state_and_fingerprint()
    {
        var sessionId=Guid.Parse("10000000-0000-0000-0000-000000000001");var teamA=Guid.Parse("20000000-0000-0000-0000-000000000001");var teamB=Guid.Parse("20000000-0000-0000-0000-000000000002");var entityA=Guid.Parse("30000000-0000-0000-0000-000000000001");var entityB=Guid.Parse("30000000-0000-0000-0000-000000000002");var guide=Guid.Parse("30000000-0000-0000-0000-000000000003");
        var events=new List<IDomainEvent>();var created=StorySession.Create(sessionId,"demo.story","1.0.0","sha256:demo",42,Meta(sessionId));events.Add(created);var live=StorySession.From(events);
        Add(live,events,live.AddTeam(teamA,"Alpha","hash-a",Meta(sessionId)));Add(live,events,live.AddTeam(teamB,"Beta","hash-b",Meta(sessionId)));
        Add(live,events,live.CreateWorldEntity(entityA,"hero.a","Hero A",ControllerType.HumanTeam,null,Meta(sessionId)));Add(live,events,live.CreateWorldEntity(entityB,"hero.b","Hero B",ControllerType.HumanTeam,null,Meta(sessionId)));Add(live,events,live.CreateWorldEntity(guide,"guide","Guide",ControllerType.AuthoredBehavior,"guide.v1",Meta(sessionId)));
        Add(live,events,live.AssignEntityToTeam(entityA,teamA,Meta(sessionId)));Add(live,events,live.AssignEntityToTeam(entityB,teamB,Meta(sessionId)));
        Add(live,events,live.SetInitialMetric(MetricScope.World,sessionId,"chapter.progress",0,Meta(sessionId)));Add(live,events,live.AddInitialMemory(MemoryScope.World,sessionId,"opening","{\"scene\":1}",MemoryVisibility.Public,Meta(sessionId)));Add(live,events,live.AddInitialMemory(MemoryScope.Team,teamA,"private.clue",null,MemoryVisibility.TeamPrivate,Meta(sessionId)));Add(live,events,live.SetInitialRelationship(entityA,entityB,"stance",1,Meta(sessionId)));
        Add(live,events,live.Start(Meta(sessionId)));Add(live,events,live.Pause(Meta(sessionId)));Add(live,events,live.Resume(Meta(sessionId)));
        var replay=StorySession.From(events);replay.Should().BeEquivalentTo(live);replay.Status.Should().Be(SessionStatus.Running);EventFingerprint.Compute(events).Should().Be(EventFingerprint.Compute(events.ToArray()));
    }
    private static EventMetadata Meta(Guid sessionId)=>new(Guid.Parse("40000000-0000-0000-0000-000000000001"),Guid.Parse("50000000-0000-0000-0000-000000000001"),Guid.NewGuid(),sessionId,null,new DateTimeOffset(2026,8,4,8,0,0,TimeSpan.Zero));
    private static void Add(StorySession session,List<IDomainEvent> history,IDomainEvent @event){history.Add(@event);session.Apply(@event);}
}
