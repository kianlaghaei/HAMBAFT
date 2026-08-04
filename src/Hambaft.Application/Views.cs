using Hambaft.Domain;

namespace Hambaft.Application;

public sealed record SessionStateView(Guid Id, string StoryPackageId, string StoryVersion, string ContentHash, int Seed, SessionStatus Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc, IReadOnlyList<Team> Teams, IReadOnlyList<WorldEntity> Entities, long StateVersion);
public sealed record TeamExperienceView(Guid Id, Guid SessionId, Team Team, WorldEntity? ControlledEntity, IReadOnlyList<Metric> VisibleMetrics, IReadOnlyList<StoryMemory> VisibleMemories, IReadOnlyList<Relationship> VisibleRelationships, long StateVersion);
public sealed record PublicWorldView(Guid Id, SessionStatus Status, IReadOnlyList<WorldEntity> Entities, IReadOnlyList<Metric> WorldMetrics, IReadOnlyList<StoryMemory> PublicMemories, IReadOnlyList<Relationship> PublicRelationships, long StateVersion);
public sealed record EntityStateView(Guid Id, Guid SessionId, ControllerType ControllerType, Guid? ControlledByTeamId, EntityStatus Status, IReadOnlyList<Metric> Metrics, IReadOnlyList<StoryMemory> VisibleMemories, long StateVersion);

public static class ViewProjector
{
    public static SessionStateView Session(StorySession s) => new(s.Id,s.StoryPackageId,s.StoryVersion,s.ContentHash,s.Seed,s.Status,s.CreatedAtUtc,s.StartedAtUtc,s.CompletedAtUtc,s.Teams.ToList(),s.Entities.ToList(),s.StateVersion);
    public static PublicWorldView Public(StorySession s) => new(s.Id,s.Status,s.Entities.ToList(),s.Metrics.Where(x=>x.Scope==MetricScope.World).ToList(),s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public).ToList(),s.Relationships.ToList(),s.StateVersion);
    public static TeamExperienceView Team(StorySession s, Team team)
    {
        var entity=s.Entities.SingleOrDefault(x=>x.Id==team.ControlledEntityId);
        var metrics=s.Metrics.Where(x=>x.Scope==MetricScope.World || x.Scope==MetricScope.Team&&x.ScopeId==team.Id || entity is not null&&x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList();
        var memories=s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public || x.Visibility==MemoryVisibility.TeamPrivate&&x.Scope==MemoryScope.Team&&x.ScopeId==team.Id || entity is not null&&x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList();
        var relationships=entity is null?[]:s.Relationships.Where(x=>x.SourceEntityId==entity.Id||x.TargetEntityId==entity.Id).ToList();
        return new(team.Id,s.Id,team,entity,metrics,memories,relationships,s.StateVersion);
    }
    public static EntityStateView Entity(StorySession s, WorldEntity entity) => new(entity.Id,s.Id,entity.ControllerType,entity.ControlledByTeamId,entity.Status,s.Metrics.Where(x=>x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList(),s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList(),s.StateVersion);
}
