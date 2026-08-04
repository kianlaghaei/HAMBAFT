using Hambaft.Application;
using Hambaft.Domain;
using Marten;

namespace Hambaft.Infrastructure.Persistence;

public sealed class MartenSessionStore : ISessionStore
{
    private readonly IDocumentSession session;
    private readonly IStoryPackageLoader? packages;
    public MartenSessionStore(IDocumentSession session) { this.session=session; }
    public MartenSessionStore(IDocumentSession session,IStoryPackageLoader packages) : this(session) { this.packages=packages; }
    public async Task<StorySession?> LoadAsync(Guid sessionId,CancellationToken ct)
    {
        var events=await LoadEventsAsync(sessionId,ct);
        return events.Count==0?null:StorySession.From(events);
    }

    public async Task<IReadOnlyList<IDomainEvent>> LoadEventsAsync(Guid sessionId,CancellationToken ct)
    {
        var events=await session.Events.FetchStreamAsync(sessionId,token:ct);
        return events.Select(x=>x.Data).OfType<IDomainEvent>().ToList();
    }

    public async Task AppendAsync(Guid sessionId,long expectedVersion,IReadOnlyList<IDomainEvent> events,StorySession state,CancellationToken ct)
    {
        if(events.Count==0) return;
        session.CorrelationId=events[0].Metadata.CorrelationId.ToString("D");
        session.CausationId=events[0].Metadata.CausationId.ToString("D");
        if(expectedVersion==0) session.Events.StartStream<StorySession>(sessionId,events.Cast<object>());
        else session.Events.Append(sessionId,expectedVersion+events.Count,events.Cast<object>());

        await session.SaveChangesAsync(ct);
    }

    public Task<SessionStateView?> LoadSessionViewAsync(Guid id,CancellationToken ct)=>session.LoadAsync<SessionStateView>(id,ct);
    public async Task<PublicWorldView?> LoadPublicViewAsync(Guid id,CancellationToken ct)
    {
        var view=await session.LoadAsync<PublicWorldView>(id,ct); if(view is null||packages is null||view.CurrentWorldNarrativeRef is null) return view;
        var package=await packages.LoadAsync(view.StoryPackageId,view.StoryVersion,ct); EnsureHash(view.ContentHash,package.ContentHash); return ViewProjector.Hydrate(view,package);
    }
    public async Task<TeamExperienceView?> LoadTeamViewAsync(Guid id,CancellationToken ct)
    {
        var experience=await session.Query<SessionExperienceView>().FirstOrDefaultAsync(x=>x.Teams.Any(team=>team.Id==id),ct);
        var team=experience?.Teams.SingleOrDefault(x=>x.Id==id);
        if(experience is null||team is null) return null;
        if(packages is null) return ViewProjector.Team(experience,team);
        var package=await packages.LoadAsync(experience.StoryPackageId,experience.StoryVersion,ct); EnsureHash(experience.ContentHash,package.ContentHash); return ViewProjector.Team(experience,team,package);
    }
    private static void EnsureHash(string expected,string actual) { if(!string.Equals(expected,actual,StringComparison.Ordinal)) throw new StoryPackageHashMismatchException(expected,actual); }
}
