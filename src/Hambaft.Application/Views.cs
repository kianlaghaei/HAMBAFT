using Hambaft.Domain;

namespace Hambaft.Application;

public sealed record NarrativeChoiceView(string Id,string Label,string? ShortOutcome);
public sealed record NarrativeContentView(string StoryletId,string NarrativeRef,string Title,IReadOnlyList<string> Paragraphs,IReadOnlyDictionary<string,string> PresentationTags);
public sealed record TeamStoryletView(Guid AssignmentId,string StoryletId,string CheckpointId,string Title,IReadOnlyList<string> Paragraphs,IReadOnlyDictionary<string,string> PresentationTags,IReadOnlyList<NarrativeChoiceView> Choices,bool RequiredResponse,bool Submitted,string? SubmittedChoiceId);

public sealed record SessionStateView(
    Guid Id,string StoryPackageId,string StoryVersion,string ContentHash,int Seed,SessionStatus Status,
    DateTimeOffset CreatedAtUtc,DateTimeOffset? StartedAtUtc,DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<Team> Teams,IReadOnlyList<WorldEntity> Entities,long StateVersion,
    string? CurrentCheckpointId,bool NarrativeInitialized,IReadOnlyList<StoryletAssignment> StoryletAssignments,IReadOnlyList<SubmittedStoryChoice> SubmittedChoices)
{
    public IReadOnlyList<Guid> TeamsAwaitingResponse=>StoryletAssignments.Where(x=>x.RequiredResponse&&x.Status==StoryletAssignmentStatus.Assigned&&x.TargetTeamId is not null).Select(x=>x.TargetTeamId!.Value).Distinct().OrderBy(x=>x).ToList();
    public IReadOnlyList<string> AssignedStoryletIds=>StoryletAssignments.Select(x=>x.StoryletId).ToList();
    public IReadOnlyList<string> SubmittedChoiceIds=>SubmittedChoices.Select(x=>x.ChoiceId).ToList();
}

public sealed record TeamExperienceView(
    Guid Id,Guid SessionId,Team Team,WorldEntity? ControlledEntity,IReadOnlyList<Metric> VisibleMetrics,
    IReadOnlyList<StoryMemory> VisibleMemories,IReadOnlyList<Relationship> VisibleRelationships,long StateVersion,
    string? CurrentCheckpointId,IReadOnlyList<TeamStoryletView> PrivateStorylets);

public sealed record PublicWorldView(
    Guid Id,SessionStatus Status,IReadOnlyList<WorldEntity> Entities,IReadOnlyList<Metric> WorldMetrics,
    IReadOnlyList<StoryMemory> PublicMemories,IReadOnlyList<Relationship> PublicRelationships,long StateVersion,
    string StoryPackageId,string StoryVersion,string ContentHash,string? CurrentCheckpointId,string? CurrentWorldStoryletId,string? CurrentWorldNarrativeRef,NarrativeContentView? Narrative);

public sealed record EntityStateView(Guid Id,Guid SessionId,ControllerType ControllerType,Guid? ControlledByTeamId,EntityStatus Status,IReadOnlyList<Metric> Metrics,IReadOnlyList<StoryMemory> VisibleMemories,long StateVersion);

public sealed record SessionExperienceView(
    Guid Id,SessionStatus Status,IReadOnlyList<Team> Teams,IReadOnlyList<WorldEntity> Entities,IReadOnlyList<Metric> Metrics,
    IReadOnlyList<StoryMemory> Memories,IReadOnlyList<Relationship> Relationships,long StateVersion,
    string StoryPackageId,string StoryVersion,string ContentHash,string? CurrentCheckpointId,
    IReadOnlyList<StoryletAssignment> StoryletAssignments,IReadOnlyList<SubmittedStoryChoice> SubmittedChoices);

public static class ViewProjector
{
    public static SessionStateView Session(StorySession s)=>new(s.Id,s.StoryPackageId,s.StoryVersion,s.ContentHash,s.Seed,s.Status,s.CreatedAtUtc,s.StartedAtUtc,s.CompletedAtUtc,s.Teams.ToList(),s.Entities.ToList(),s.StateVersion,s.CurrentCheckpointId,s.NarrativeInitialized,s.StoryletAssignments.ToList(),s.SubmittedChoices.ToList());
    public static PublicWorldView Public(StorySession s)=>new(s.Id,s.Status,s.Entities.ToList(),s.Metrics.Where(x=>x.Scope==MetricScope.World).ToList(),s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public).ToList(),s.Relationships.ToList(),s.StateVersion,s.StoryPackageId,s.StoryVersion,s.ContentHash,s.CurrentCheckpointId,s.CurrentWorldStoryletId,s.CurrentWorldNarrativeRef,null);
    public static TeamExperienceView Team(StorySession s,Team team,StoryPackage? package=null,string? locale=null)
    {
        var entity=s.Entities.SingleOrDefault(x=>x.Id==team.ControlledEntityId);
        var metrics=s.Metrics.Where(x=>x.Scope==MetricScope.World||x.Scope==MetricScope.Team&&x.ScopeId==team.Id||entity is not null&&x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList();
        var memories=s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.TeamPrivate&&x.Scope==MemoryScope.Team&&x.ScopeId==team.Id||entity is not null&&x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList();
        var relationships=entity is null?[]:s.Relationships.Where(x=>x.SourceEntityId==entity.Id||x.TargetEntityId==entity.Id).ToList();
        return new(team.Id,s.Id,team,entity,metrics,memories,relationships,s.StateVersion,s.CurrentCheckpointId,package is null?[]:PrivateStorylets(s.StoryletAssignments,s.SubmittedChoices,team,entity,package,locale));
    }
    public static EntityStateView Entity(StorySession s,WorldEntity entity)=>new(entity.Id,s.Id,entity.ControllerType,entity.ControlledByTeamId,entity.Status,s.Metrics.Where(x=>x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList(),s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList(),s.StateVersion);
    public static TeamExperienceView Team(SessionExperienceView s,Team team,StoryPackage? package=null,string? locale=null)
    {
        var entity=s.Entities.SingleOrDefault(x=>x.Id==team.ControlledEntityId);
        var metrics=s.Metrics.Where(x=>x.Scope==MetricScope.World||x.Scope==MetricScope.Team&&x.ScopeId==team.Id||entity is not null&&x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList();
        var memories=s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.TeamPrivate&&x.Scope==MemoryScope.Team&&x.ScopeId==team.Id||entity is not null&&x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList();
        var relationships=entity is null?[]:s.Relationships.Where(x=>x.SourceEntityId==entity.Id||x.TargetEntityId==entity.Id).ToList();
        return new(team.Id,s.Id,team,entity,metrics,memories,relationships,s.StateVersion,s.CurrentCheckpointId,package is null?[]:PrivateStorylets(s.StoryletAssignments,s.SubmittedChoices,team,entity,package,locale));
    }
    public static EntityStateView Entity(SessionExperienceView s,WorldEntity entity)=>new(entity.Id,s.Id,entity.ControllerType,entity.ControlledByTeamId,entity.Status,s.Metrics.Where(x=>x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList(),s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList(),s.StateVersion);
    public static PublicWorldView Hydrate(PublicWorldView view,StoryPackage package,string? locale=null)=>view with { Narrative=view.CurrentWorldStoryletId is null||view.CurrentWorldNarrativeRef is null?null:Content(package,view.CurrentWorldStoryletId,view.CurrentWorldNarrativeRef,locale) };

    private static IReadOnlyList<TeamStoryletView> PrivateStorylets(IReadOnlyList<StoryletAssignment> assignments,IReadOnlyList<SubmittedStoryChoice> submissions,Team team,WorldEntity? entity,StoryPackage package,string? locale)
        =>assignments.Where(a=>(a.Status is StoryletAssignmentStatus.Assigned or StoryletAssignmentStatus.Responded)&&(a.TargetTeamId==team.Id||entity is not null&&a.Scope==StoryletScope.EntityPrivate&&a.TargetEntityId==entity.Id))
            .OrderBy(a=>a.AssignmentId).Select(a=>
            {
                var storylet=package.Storylets.Single(s=>s.Id==a.StoryletId); var narrative=Localized(package,storylet.NarrativeRef,locale); var submitted=submissions.SingleOrDefault(s=>s.AssignmentId==a.AssignmentId);
                return new TeamStoryletView(a.AssignmentId,a.StoryletId,a.CheckpointId,narrative.Title,narrative.Paragraphs,narrative.PresentationTags,storylet.Choices.Select(c=>{var label=Localized(package,c.LabelRef,locale);return new NarrativeChoiceView(c.Id,label.ChoiceLabel??label.Title,label.ShortOutcome);}).ToList(),a.RequiredResponse,submitted is not null,submitted?.ChoiceId);
            }).ToList();
    private static NarrativeContentView Content(StoryPackage package,string storyletId,string narrativeRef,string? locale)
    { var n=Localized(package,narrativeRef,locale); return new(storyletId,narrativeRef,n.Title,n.Paragraphs,n.PresentationTags); }
    private static NarrativeDefinition Localized(StoryPackage package,string key,string? locale)
    {
        var selected=locale is not null&&package.Narrative.ContainsKey(locale)?locale:package.Manifest.DefaultLocale;
        if(!package.Narrative.TryGetValue(selected,out var dictionary)||!dictionary.TryGetValue(key,out var value)) throw new DomainException($"Narrative reference '{key}' is missing for locale '{selected}'.");
        return value;
    }
}
