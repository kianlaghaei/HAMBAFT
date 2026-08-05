using System.Security.Cryptography;
using System.Text;
using Hambaft.Domain;

namespace Hambaft.Application;

public sealed record SemanticPresentationResult(
    WorldPresentationStateView World,
    BusinessPresentationStateView? Business,
    IReadOnlyList<RelationshipPresentationStateView> Relationships);

public static class SemanticPresentation
{
    public static SemanticPresentationResult Project(
        StoryPackage package,
        Guid sessionId,
        string? checkpointId,
        IReadOnlyList<WorldEntity> entities,
        IReadOnlyList<Metric> visibleMetrics,
        IReadOnlyList<Relationship> visibleRelationships,
        bool teamView)
    {
        var authored=package.PresentationDefinition;
        var scene=authored.Scenes.FirstOrDefault(x=>x.CheckpointId==checkpointId)
            ??authored.Scenes.FirstOrDefault()
            ??new("waiting",checkpointId??"waiting","morning","بازار","بازار هنوز آماده نشده است",string.Empty,string.Empty,"silent",false,[],new Dictionary<string,string>(),[]);
        var semanticMetrics=visibleMetrics.Where(x=>x.Scope==MetricScope.World)
            .Select(metric=>Map(metric,authored.MetricBands.SingleOrDefault(x=>x.Scope==metric.Scope&&x.MetricKey==metric.MetricKey)?.Bands))
            .Where(x=>x is not null).Cast<SemanticStateView>().OrderBy(x=>x.Key,StringComparer.Ordinal).ToList();
        var activeTags=scene.VisualTags.Concat(semanticMetrics.SelectMany(x=>x.VisualTags)).ToHashSet(StringComparer.Ordinal);
        var locations=authored.Locations.Select(location=>
        {
            var entity=location.EntityDefinitionId is null?null:entities.SingleOrDefault(x=>x.DefinitionId==location.EntityDefinitionId);
            var current=scene.LocationConditions.GetValueOrDefault(location.Id,location.PublicCondition);
            return new LocationPresentationStateView(location.Id,location.DisplayName,location.ShortIdentity,location.WhyItMatters,current,location.WhoIsHere,location.RecentChange,location.AvailableActions,location.PresentationTags,location.X,location.Y,entity?.Id,location.EntityDefinitionId);
        }).ToList();
        var publicBusinesses=locations.Where(x=>x.EntityId is not null).Select(location=>new BusinessPresentationStateView(
            location.EntityId!.Value,location.EntityDefinitionId!,location.DisplayName,[location.CurrentCondition],location.PresentationTags.Concat(scene.VisualTags).Distinct(StringComparer.Ordinal).ToList())).ToList();
        var ownEntity=visibleMetrics.Where(x=>x.Scope==MetricScope.Entity).Select(x=>entities.SingleOrDefault(e=>e.Id==x.ScopeId)).FirstOrDefault(x=>x is not null);
        BusinessPresentationStateView? business=null;
        if(teamView&&ownEntity is not null)
        {
            var businessPulse=new List<string>();var tags=new HashSet<string>(StringComparer.Ordinal);
            foreach(var metric in visibleMetrics.Where(x=>x.Scope==MetricScope.Entity&&x.ScopeId==ownEntity.Id))
            {
                var definition=authored.BusinessStates.SingleOrDefault(x=>x.EntityDefinitionId==ownEntity.DefinitionId&&x.MetricKey==metric.MetricKey);
                var state=Map(metric,definition?.Bands);if(state is null)continue;businessPulse.Add(state.Label);foreach(var tag in state.VisualTags)tags.Add(tag);
            }
            business=new(ownEntity.Id,ownEntity.DefinitionId,ownEntity.DisplayName,businessPulse,tags.ToList());
            publicBusinesses=publicBusinesses.Select(x=>x.EntityId==business.EntityId?business:x).ToList();
        }
        var relationships=visibleRelationships.Select(value=>
        {
            var definition=authored.RelationshipStates.SingleOrDefault(x=>x.RelationshipKey==value.RelationshipKey);
            var band=definition?.Bands.SingleOrDefault(x=>value.NumericValue>=x.Minimum&&value.NumericValue<=x.Maximum);
            return band is null?null:new RelationshipPresentationStateView(value.SourceEntityId,value.TargetEntityId,value.RelationshipKey,band.Id,band.Label,band.Description,band.VisualTags);
        }).Where(x=>x is not null).Cast<RelationshipPresentationStateView>().ToList();
        var characters=authored.Characters.Where(x=>scene.CharacterIds.Contains(x.Id)&&x.Visibility.Equals("Public",StringComparison.OrdinalIgnoreCase))
            .Select(x=>new CharacterPresentationStateView(x.Id,x.DisplayName,x.LocationId,x.WhatIsKnown,x.LastSeen,x.Attitude,x.RecentStatement,x.PossibleInteraction,x.PresentationTags)).ToList();
        var ambient=authored.AmbientEvents.Where(x=>x.Visibility.Equals("Public",StringComparison.OrdinalIgnoreCase)&&x.RequiredVisualTags.All(activeTags.Contains)&&!x.ForbiddenVisualTags.Any(activeTags.Contains))
            .OrderBy(x=>DeterministicOrder(package.ContentHash,sessionId,scene.Id,x.Id),StringComparer.Ordinal).Take(3)
            .Select(x=>new AmbientPresentationStateView(x.Id,x.Description,x.LocationId)).ToList();
        var reactions=authored.Reactions.Where(x=>x.CheckpointId==checkpointId).Select(x=>new ReactionPresentationStateView(x.OutcomeLine,x.LocationIds,x.VisualTags)).Take(3).ToList();
        var pulse=semanticMetrics.Select(x=>x.Label).ToList();
        if(!string.IsNullOrWhiteSpace(scene.PublicEvent))pulse.Add(scene.PublicEvent);
        var world=new WorldPresentationStateView(scene.Id,scene.TimeOfDay,scene.TimeLabel,scene.AtmosphereLabel,scene.PublicEvent,scene.CourtyardActivity,scene.Soundscape,scene.AvanVisible,scene.VisualTags,pulse,semanticMetrics,locations,publicBusinesses,characters,ambient,reactions);
        return new(world,business,relationships);
    }

    public static SemanticStateView? Map(Metric metric,IReadOnlyList<PresentationBandDefinition>? bands)
    {
        var band=bands?.SingleOrDefault(x=>metric.NumericValue>=x.Minimum&&metric.NumericValue<=x.Maximum);
        return band is null?null:new(metric.MetricKey,band.Id,band.Label,band.Description,band.VisualTags);
    }

    private static string DeterministicOrder(string contentHash,Guid sessionId,string sceneId,string eventId)
        =>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{contentHash}|{sessionId:D}|{sceneId}|{eventId}")));
}
