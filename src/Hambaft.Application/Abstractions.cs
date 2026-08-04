using Hambaft.Domain;

namespace Hambaft.Application;

public interface ISessionStore
{
    Task<StorySession?> LoadAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<IDomainEvent>> LoadEventsAsync(Guid sessionId, CancellationToken cancellationToken);
    Task AppendAsync(Guid sessionId, long expectedVersion, IReadOnlyList<IDomainEvent> events, StorySession projectedState, CancellationToken cancellationToken);
    Task<SessionStateView?> LoadSessionViewAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<PublicWorldView?> LoadPublicViewAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<TeamExperienceView?> LoadTeamViewAsync(Guid teamId, CancellationToken cancellationToken);
}

public sealed record PairingCodeMaterial(string RawCode, string Hash);
public interface IPairingCodeGenerator { PairingCodeMaterial Generate(); bool Verify(string rawCode, string persistedHash); }
