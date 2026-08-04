using FluentAssertions;
using Hambaft.Domain;

namespace Hambaft.UnitTests;

public sealed class StorySessionTests
{
    private readonly Guid _sessionId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly DateTimeOffset _now = new(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Session_creation_has_versioned_package_metadata()
    {
        var created = StorySession.Create(_sessionId, "story.alpha", "1.0.0", "sha256:abc", 42, Meta());
        var session = StorySession.From([created]);
        session.Should().BeEquivalentTo(new { Id=_sessionId, StoryPackageId="story.alpha", StoryVersion="1.0.0", ContentHash="sha256:abc", Seed=42, Status=SessionStatus.Created, StateVersion=1L });
    }

    [Theory]
    [InlineData("", "1", "hash")]
    [InlineData("pkg", "", "hash")]
    [InlineData("pkg", "1", "")]
    public void Missing_package_metadata_is_rejected(string package, string version, string hash) =>
        FluentActions.Invoking(()=>StorySession.Create(_sessionId,package,version,hash,1,Meta())).Should().Throw<DomainException>();

    [Fact]
    public void Team_model_contains_no_individual_records()
    {
        typeof(Team).GetProperties().Select(x=>x.Name).Should().Equal("Id","SessionId","DisplayName","PairingCodeHash","ControlledEntityId","JoinedAtUtc");
    }

    [Fact]
    public void Human_entity_must_be_assigned_before_start()
    {
        var (session, events, teamId) = SetupWithTeam();
        Add(session,events,session.CreateWorldEntity(Guid.NewGuid(),"human","Human",ControllerType.HumanTeam,null,Meta()));
        FluentActions.Invoking(()=>session.Start(Meta())).Should().Throw<DomainException>().WithMessage("*controlling Team*");
        teamId.Should().NotBeEmpty();
    }

    [Fact]
    public void Authored_behavior_requires_profile()
    {
        var (session,_,_) = SetupWithTeam();
        FluentActions.Invoking(()=>session.CreateWorldEntity(Guid.NewGuid(),"npc","NPC",ControllerType.AuthoredBehavior,null,Meta())).Should().Throw<DomainException>();
    }

    [Fact]
    public void Assignment_is_unique_for_both_team_and_entity()
    {
        var (session,events,teamId)=SetupWithTeam();
        var first=Guid.NewGuid(); var second=Guid.NewGuid();
        Add(session,events,session.CreateWorldEntity(first,"one","One",ControllerType.HumanTeam,null,Meta()));
        Add(session,events,session.CreateWorldEntity(second,"two","Two",ControllerType.HumanTeam,null,Meta()));
        Add(session,events,session.AssignEntityToTeam(first,teamId,Meta()));
        FluentActions.Invoking(()=>session.AssignEntityToTeam(second,teamId,Meta())).Should().Throw<DomainException>();
        FluentActions.Invoking(()=>session.AssignEntityToTeam(first,teamId,Meta())).Should().Throw<DomainException>();
    }

    [Fact]
    public void Metric_key_is_normalized_and_unique_in_scope()
    {
        var (session,events,_)=SetupWithTeam();
        Add(session,events,session.SetInitialMetric(MetricScope.World,_sessionId,"  Chapter.Progress ",1,Meta()));
        session.Metrics.Single().MetricKey.Should().Be("chapter.progress");
        FluentActions.Invoking(()=>session.SetInitialMetric(MetricScope.World,_sessionId,"CHAPTER.PROGRESS",2,Meta())).Should().Throw<DomainException>();
    }

    [Fact]
    public void Relationships_are_directional()
    {
        var (session,events,_)=SetupWithTeam(); var a=Guid.NewGuid(); var b=Guid.NewGuid();
        Add(session,events,session.CreateWorldEntity(a,"a","A",ControllerType.System,null,Meta()));
        Add(session,events,session.CreateWorldEntity(b,"b","B",ControllerType.System,null,Meta()));
        Add(session,events,session.SetInitialRelationship(a,b,"affinity",1,Meta()));
        Add(session,events,session.SetInitialRelationship(b,a,"affinity",2,Meta()));
        session.Relationships.Should().HaveCount(2).And.Contain(x=>x.SourceEntityId==b && x.NumericValue==2);
    }

    [Fact]
    public void Memory_visibility_requires_matching_private_scope()
    {
        var (session,events,teamId)=SetupWithTeam();
        Add(session,events,session.AddInitialMemory(MemoryScope.Team,teamId,"clue",null,MemoryVisibility.TeamPrivate,Meta()));
        session.Memories.Single().Visibility.Should().Be(MemoryVisibility.TeamPrivate);
        FluentActions.Invoking(()=>session.AddInitialMemory(MemoryScope.World,_sessionId,"bad",null,MemoryVisibility.TeamPrivate,Meta())).Should().Throw<DomainException>();
    }

    [Fact]
    public void Start_pause_resume_are_valid_transitions()
    {
        var (session,events,teamId)=SetupWithTeam(); var entity=Guid.NewGuid();
        Add(session,events,session.CreateWorldEntity(entity,"hero","Hero",ControllerType.HumanTeam,null,Meta()));
        Add(session,events,session.AssignEntityToTeam(entity,teamId,Meta()));
        Add(session,events,session.Start(Meta())); Add(session,events,session.Pause(Meta())); Add(session,events,session.Resume(Meta()));
        session.Status.Should().Be(SessionStatus.Running);
    }

    [Fact]
    public void Invalid_transition_is_rejected()
    {
        var (session,_,_)=SetupWithTeam();
        FluentActions.Invoking(()=>session.Pause(Meta())).Should().Throw<DomainException>();
    }

    [Fact]
    public void Fingerprint_is_deterministic_and_order_sensitive()
    {
        var a=StorySession.Create(_sessionId,"pkg","1","hash",7,Meta()); var b=new SessionCancelled(Meta());
        EventFingerprint.Compute([a,b]).Should().Be(EventFingerprint.Compute([a,b])).And.NotBe(EventFingerprint.Compute([b,a]));
    }

    [Fact]
    public void Replay_is_pure_and_reproduces_state()
    {
        var (live,events,teamId)=SetupWithTeam(); var entity=Guid.NewGuid();
        Add(live,events,live.CreateWorldEntity(entity,"hero","Hero",ControllerType.HumanTeam,null,Meta()));
        Add(live,events,live.AssignEntityToTeam(entity,teamId,Meta())); Add(live,events,live.Start(Meta()));
        var replayed=StorySession.From(events);
        replayed.Should().BeEquivalentTo(live);
    }

    private (StorySession Session,List<IDomainEvent> Events,Guid TeamId) SetupWithTeam()
    {
        var events=new List<IDomainEvent>(); var created=StorySession.Create(_sessionId,"pkg","1","hash",7,Meta()); events.Add(created); var session=StorySession.From(events); var team=Guid.NewGuid(); Add(session,events,session.AddTeam(team,"Team","hash",Meta())); return(session,events,team);
    }
    private EventMetadata Meta()=>new(Guid.Parse("20000000-0000-0000-0000-000000000001"),Guid.Parse("30000000-0000-0000-0000-000000000001"),Guid.NewGuid(),_sessionId,null,_now);
    private static void Add(StorySession session,List<IDomainEvent> events,IDomainEvent @event){events.Add(@event);session.Apply(@event);}
}
