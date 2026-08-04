using Hambaft.Application;
using Hambaft.Domain;
using Marten;
using Hambaft.Narrative.Ink;

namespace Hambaft.Infrastructure.Persistence;

public sealed class MartenSessionStore : ISessionStore
{
    private readonly IDocumentSession session;
    private readonly IStoryPackageLoader? packages;
    private readonly IInkNarrativeRenderer? ink;
    public MartenSessionStore(IDocumentSession session) { this.session=session; }
    public MartenSessionStore(IDocumentSession session,IStoryPackageLoader packages) : this(session) { this.packages=packages; }
    public MartenSessionStore(IDocumentSession session,IStoryPackageLoader packages,IInkNarrativeRenderer ink) : this(session,packages) { this.ink=ink; }
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
        var package=await packages.LoadAsync(view.StoryPackageId,view.StoryVersion,ct); EnsureHash(view.ContentHash,package.ContentHash);
        var hydrated=ViewProjector.Hydrate(view,package);
        if(ink is not null&&view.CurrentWorldNarrativeRef is { } narrative&&package.InkDefinition.References.Any(x=>x.Id==narrative))
        {
            var experience=await session.LoadAsync<SessionExperienceView>(id,ct);
            if(experience is not null)
            {
                var rendered=await Render(package,narrative,StoryletScope.WorldPublic,experience,null,null,null,ct);
                hydrated=hydrated with { Narrative=new(view.CurrentWorldStoryletId??string.Empty,narrative,string.Empty,rendered.Paragraphs,Tags(rendered.Tags)) };
            }
        }
        if(ink is not null)
        {
            var experience=await session.LoadAsync<SessionExperienceView>(id,ct);
            if(experience is not null)hydrated=await HydratePublicEndings(hydrated,experience,package,ct);
        }
        return hydrated;
    }
    public async Task<TeamExperienceView?> LoadTeamViewAsync(Guid id,CancellationToken ct)
    {
        var experience=await session.Query<SessionExperienceView>().FirstOrDefaultAsync(x=>x.Teams.Any(team=>team.Id==id),ct);
        var team=experience?.Teams.SingleOrDefault(x=>x.Id==id);
        if(experience is null||team is null) return null;
        if(packages is null) return ViewProjector.Team(experience,team);
        var package=await packages.LoadAsync(experience.StoryPackageId,experience.StoryVersion,ct); EnsureHash(experience.ContentHash,package.ContentHash);
        var view=ViewProjector.Team(experience,team,package);
        if(ink is null)return view;
        var entity=experience.Entities.SingleOrDefault(x=>x.Id==team.ControlledEntityId);
        var storylets=new List<TeamStoryletView>();
        foreach(var item in view.PrivateStorylets)
        {
            var definition=package.Storylets.Single(x=>x.Id==item.StoryletId);
            if(!package.InkDefinition.References.Any(x=>x.Id==definition.NarrativeRef)){storylets.Add(item);continue;}
            var rendered=await Render(package,definition.NarrativeRef,definition.Scope,experience,team,entity,null,ct);
            var choices=new List<NarrativeChoiceView>();
            foreach(var choice in definition.Choices)
            {
                if(package.InkDefinition.References.Any(x=>x.Id==choice.LabelRef))
                {
                    var label=await Render(package,choice.LabelRef,definition.Scope,experience,team,entity,null,ct);
                    choices.Add(new(choice.Id,string.Join(" ",label.Paragraphs),null));
                }
                else choices.Add(item.Choices.Single(x=>x.Id==choice.Id));
            }
            storylets.Add(item with { Title=rendered.Tags.FirstOrDefault(x=>x.Name=="scene")?.Value??string.Empty,Paragraphs=rendered.Paragraphs,PresentationTags=Tags(rendered.Tags),Choices=choices });
        }
        view=view with { PrivateStorylets=storylets };
        var own=(experience.EndingResults??[]).SingleOrDefault(x=>x.Scope==EndingScope.Entity&&x.ScopeId==entity?.Id);
        var world=(experience.EndingResults??[]).SingleOrDefault(x=>x.Scope==EndingScope.World);
        if(own is not null)view=view with { EntityEnding=await HydrateEnding(own,package,experience,team,entity,false,ct) };
        if(world is not null)view=view with { WorldEnding=await HydrateEnding(world,package,experience,team,entity,true,ct) };
        return view;
    }
    public Task<EndingEvidenceView?> LoadEndingEvidenceAsync(Guid id,CancellationToken ct)=>session.LoadAsync<EndingEvidenceView>(id,ct);

    private async Task<PublicWorldView> HydratePublicEndings(PublicWorldView view,SessionExperienceView experience,StoryPackage package,CancellationToken ct)
    {
        var world=(experience.EndingResults??[]).SingleOrDefault(x=>x.Scope==EndingScope.World);
        var worldView=world is null?null:await HydrateEnding(world,package,experience,null,null,true,ct);
        var summaries=new List<EndingPresentationView>();
        foreach(var result in (experience.EndingResults??[]).Where(x=>x.Scope==EndingScope.Entity&&x.PublicSummaryNarrativeRef is not null).OrderBy(x=>x.ScopeId))
        {
            var entity=experience.Entities.Single(x=>x.Id==result.ScopeId);
            var rendered=await Render(package,result.PublicSummaryNarrativeRef!,StoryletScope.WorldPublic,experience,null,entity,result,ct);
            summaries.Add(new(result.EndingResultId,result.Scope,result.ScopeId,result.EndingDefinitionId,string.Empty,rendered.Paragraphs,Tags(rendered.Tags),[],result.ContentHash,result.ResolvedAtStreamVersion));
        }
        return view with { WorldEnding=worldView,PublicEntityEndingSummaries=summaries };
    }

    private async Task<EndingPresentationView> HydrateEnding(EndingResult result,StoryPackage package,SessionExperienceView experience,Team? team,WorldEntity? entity,bool publicEvidence,CancellationToken ct)
    {
        string titleRef;
        if(result.Scope==EndingScope.Entity)titleRef=package.EntityEndingDefinitions.Single(x=>x.Id==result.EndingDefinitionId).TitleNarrativeRef;
        else titleRef=package.WorldEndingDefinitions.Single(x=>x.Id==result.EndingDefinitionId).TitleNarrativeRef;
        var scope=result.Scope==EndingScope.World?StoryletScope.WorldPublic:StoryletScope.EntityPrivate;
        var title=await Render(package,titleRef,scope,experience,team,entity,result,ct);
        var body=await Render(package,result.NarrativeRef,scope,experience,team,entity,result,ct);
        var evidence=publicEvidence?result.Evidence.Where(x=>x.IsPublic).ToList():result.Evidence;
        return new(result.EndingResultId,result.Scope,result.ScopeId,result.EndingDefinitionId,string.Join(" ",title.Paragraphs),body.Paragraphs,result.PresentationTags.Concat(Tags(body.Tags)).GroupBy(x=>x.Key,StringComparer.Ordinal).ToDictionary(x=>x.Key,x=>x.Last().Value,StringComparer.Ordinal),evidence,result.ContentHash,result.ResolvedAtStreamVersion);
    }

    private async Task<InkRenderedNarrative> Render(StoryPackage package,string narrativeRef,StoryletScope scope,SessionExperienceView experience,Team? team,WorldEntity? entity,EndingResult? ending,CancellationToken ct)
    {
        var reference=package.InkDefinition.References.Single(x=>x.Id==narrativeRef);var values=new Dictionary<string,object?>(StringComparer.Ordinal);
        foreach(var name in reference.RequiredVariables)
        {
            values[name]=name switch
            {
                "entity_display_name"=>entity?.DisplayName??string.Empty,
                "team_display_name"=>team?.DisplayName??string.Empty,
                "current_checkpoint"=>experience.CurrentCheckpointId??string.Empty,
                "difficulty"=>experience.DifficultyId,
                "ending_id"=>ending?.EndingDefinitionId??string.Empty,
                "current_checkpoint_number"=>experience.CheckpointResolutionNumber,
                _=>Scalar(experience,entity,team,name,package.InkDefinition.Variables.Single(x=>x.Name==name).Type)
            };
        }
        return await ink!.RenderAsync(new(package,narrativeRef,scope,values),ct);
    }

    private static object Scalar(SessionExperienceView experience,WorldEntity? entity,Team? team,string name,InkScalarType type)
    {
        var normalized=name.Replace("_",string.Empty,StringComparison.Ordinal);
        var metric=experience.Metrics.FirstOrDefault(x=>(x.Scope==MetricScope.World||entity is not null&&x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id||team is not null&&x.Scope==MetricScope.Team&&x.ScopeId==team.Id)&&x.MetricKey.Replace("_",string.Empty,StringComparison.Ordinal).Equals(normalized,StringComparison.OrdinalIgnoreCase));
        if(metric is not null)return type==InkScalarType.Integer?(object)(int)metric.NumericValue:metric.NumericValue;
        if(type==InkScalarType.Boolean)return experience.Memories.Any(x=>x.Key.Replace("_",string.Empty,StringComparison.Ordinal).Equals(normalized,StringComparison.OrdinalIgnoreCase));
        return type switch { InkScalarType.String=>string.Empty,InkScalarType.Integer=>0,InkScalarType.Decimal=>0m,InkScalarType.Boolean=>false,_=>string.Empty };
    }
    private static IReadOnlyDictionary<string,string> Tags(IEnumerable<InkTag> tags)=>tags.GroupBy(x=>x.Name,StringComparer.Ordinal).ToDictionary(x=>x.Key,x=>x.Last().Value,StringComparer.Ordinal);
    private static void EnsureHash(string expected,string actual) { if(!string.Equals(expected,actual,StringComparison.Ordinal)) throw new StoryPackageHashMismatchException(expected,actual); }
}
