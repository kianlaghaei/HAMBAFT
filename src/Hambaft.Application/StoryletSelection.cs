using System.Security.Cryptography;
using System.Text;
using Hambaft.Domain;

namespace Hambaft.Application;

public sealed record StoryletSelectionContext(string CheckpointId,StoryletScope Scope,Guid? TargetTeamId,Guid? TargetEntityId,long StreamVersion);

public interface IStoryletSelector
{
    StoryletDefinition? Select(StoryPackage package,StorySession state,StoryletSelectionContext context);
}

public sealed class DeterministicStoryletSelector(IConditionEngine conditions) : IStoryletSelector
{
    public StoryletDefinition? Select(StoryPackage package,StorySession state,StoryletSelectionContext context)
    {
        var candidates=package.Storylets
            .Where(s=>s.CheckpointId==context.CheckpointId&&s.Scope==context.Scope)
            .Where(s=>TargetMatches(s.TargetSelector,state,context))
            .Where(s=>RepeatAllowed(s,state,context.CheckpointId))
            .Where(s=>conditions.EvaluateAll(s.Conditions,new(state,package,context.TargetTeamId,context.TargetEntityId),false).Result)
            .ToList();
        if(candidates.Count==0) return null;
        var highest=candidates.Max(x=>x.Priority);
        var ordered=candidates.Where(x=>x.Priority==highest).OrderBy(x=>x.Id,StringComparer.Ordinal).ToList();
        if(ordered.Count==1) return ordered[0];
        var total=ordered.Sum(x=>x.Weight);
        var material=string.Join("|",package.ContentHash,state.Seed,context.StreamVersion,context.CheckpointId,context.Scope,context.TargetTeamId,context.TargetEntityId,string.Join(",",ordered.Select(x=>x.Id)));
        var bytes=SHA256.HashData(Encoding.UTF8.GetBytes(material));
        var point=(int)(BitConverter.ToUInt64(bytes,0)%(ulong)total);
        foreach(var candidate in ordered) { if(point<candidate.Weight) return candidate; point-=candidate.Weight; }
        return ordered[^1];
    }

    private static bool TargetMatches(TargetSelectorDefinition selector,StorySession state,StoryletSelectionContext context)=>selector.Type switch
    {
        TargetSelectorType.World=>context.Scope==StoryletScope.WorldPublic&&context.TargetTeamId is null&&context.TargetEntityId is null,
        TargetSelectorType.AllTeams=>context.TargetTeamId is not null,
        TargetSelectorType.TeamControllingEntityDefinition=>context.TargetTeamId is { } team&&state.Entities.Any(e=>e.ControlledByTeamId==team&&e.DefinitionId==selector.EntityDefinitionId),
        TargetSelectorType.EntityDefinition=>context.TargetEntityId is { } entity&&state.Entities.Any(e=>e.Id==entity&&e.DefinitionId==selector.EntityDefinitionId),
        _=>false
    };
    private static bool RepeatAllowed(StoryletDefinition storylet,StorySession state,string checkpoint)=>storylet.RepeatPolicy switch
    {
        RepeatPolicy.OncePerSession=>!state.PreviouslyAssignedStoryletIds.Contains(storylet.Id),
        RepeatPolicy.OncePerCheckpoint=>!state.StoryletAssignments.Any(x=>x.StoryletId==storylet.Id&&x.CheckpointId==checkpoint),
        RepeatPolicy.Repeatable=>true,
        _=>false
    };
}

public static class DeterministicIds
{
    public static Guid Create(params object?[] parts)
    {
        var bytes=SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|",parts.Select(x=>x?.ToString()??string.Empty))));
        Span<byte> value=stackalloc byte[16]; bytes.AsSpan(0,16).CopyTo(value); value[7]=(byte)((value[7]&0x0f)|0x50); value[8]=(byte)((value[8]&0x3f)|0x80); return new Guid(value);
    }
}
