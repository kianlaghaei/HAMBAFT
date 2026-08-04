using Hambaft.Application;
using Hambaft.Domain;
using Marten;

namespace Hambaft.Infrastructure.Persistence;

public sealed class MartenSessionStore(IDocumentSession session) : ISessionStore
{
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
    public Task<PublicWorldView?> LoadPublicViewAsync(Guid id,CancellationToken ct)=>session.LoadAsync<PublicWorldView>(id,ct);
    public async Task<TeamExperienceView?> LoadTeamViewAsync(Guid id,CancellationToken ct)
    {
        var experience=await session.Query<SessionExperienceView>().FirstOrDefaultAsync(x=>x.Teams.Any(team=>team.Id==id),ct);
        var team=experience?.Teams.SingleOrDefault(x=>x.Id==id);
        return experience is null||team is null?null:ViewProjector.Team(experience,team);
    }
}
