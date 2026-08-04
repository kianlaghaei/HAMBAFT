using System.Collections.Concurrent;
using Hambaft.Domain;
using Ink.Runtime;
using IOPath=System.IO.Path;

namespace Hambaft.Narrative.Ink;

public sealed class FileInkStoryLoader(InkStoryRootOptions options) : IInkStoryLoader
{
    private readonly ConcurrentDictionary<(string Id,string Version,string Hash,string File),string> compiled=new();

    public async Task<InkStoryHandle> LoadAsync(StoryPackage package,string narrativeReference,StoryletScope targetScope,CancellationToken ct=default)
    {
        var reference=package.InkDefinition.References.SingleOrDefault(x=>x.Id==narrativeReference)
            ??throw new InkContractException($"Ink narrative reference '{narrativeReference}' is not declared.");
        if(reference.Scope!=targetScope&&!(targetScope==StoryletScope.EntityPrivate&&reference.Scope==StoryletScope.TeamPrivate))
            throw new InkContractException($"Ink narrative reference '{narrativeReference}' is not approved for {targetScope} rendering.");
        var root=IOPath.GetFullPath(options.Root);
        var packageDirectory=IOPath.GetFullPath(IOPath.Combine(root,package.Manifest.Id,package.Manifest.Version));
        var path=IOPath.GetFullPath(IOPath.Combine(packageDirectory,reference.CompiledFile.Replace('/',IOPath.DirectorySeparatorChar)));
        if(!path.StartsWith(packageDirectory+IOPath.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!File.Exists(path))
            throw new InkContractException($"Compiled Ink file '{reference.CompiledFile}' is missing or outside the package.");
        var key=(package.Manifest.Id,package.Manifest.Version,package.ContentHash,reference.CompiledFile);
        if(!compiled.TryGetValue(key,out var json))
        {
            json=await File.ReadAllTextAsync(path,ct);
            _=new Story(json);
            compiled.TryAdd(key,json);
        }
        var story=new Story(json);
        try { story.ChoosePathString(reference.EntryPoint); }
        catch(Exception ex) { throw new InkContractException($"Ink entry point '{reference.EntryPoint}' is missing: {ex.Message}"); }
        return new(story,package,narrativeReference,targetScope);
    }
}

public sealed class InkStateSerializer : IInkStateSerializer
{
    public InkStateEnvelope Serialize(InkStoryHandle story,string? targetId,string checkpoint)
        =>new(story.PackageId,story.PackageVersion,story.ContentHash,story.NarrativeReference,story.TargetScope,targetId,checkpoint,story.Runtime.state.ToJson());

    public void Restore(InkStoryHandle story,InkStateEnvelope state)
    {
        if(state.PackageId!=story.PackageId||state.PackageVersion!=story.PackageVersion||state.ContentHash!=story.ContentHash||state.NarrativeReference!=story.NarrativeReference||state.TargetScope!=story.TargetScope)
            throw new InkContractException("Ink state does not match the locked package hash, narrative reference, or target scope.");
        story.Runtime.state.LoadJson(state.RuntimeStateJson);
    }
}

public sealed class InkNarrativeRenderer(IInkStoryLoader loader,IInkStateSerializer serializer) : IInkNarrativeRenderer
{
    public async Task<InkRenderedNarrative> RenderAsync(InkRenderRequest request,CancellationToken ct=default)
    {
        var story=await loader.LoadAsync(request.Package,request.NarrativeReference,request.TargetScope,ct);
        if(request.State is not null) serializer.Restore(story,request.State);
        AssignInputs(story.Runtime,request);
        var paragraphs=new List<string>();var tags=new List<InkTag>();
        Continue(story.Runtime,request.Package,paragraphs,tags);
        var choices=ReadChoices(story.Runtime,request.Package);
        if(request.ChoiceIndex is { } index)
        {
            if(choices.All(x=>x.Index!=index))throw new InkContractException($"Ink choice index {index} is not available.");
            story.Runtime.ChooseChoiceIndex(index);Continue(story.Runtime,request.Package,paragraphs,tags);choices=ReadChoices(story.Runtime,request.Package);
        }
        var state=serializer.Serialize(story,request.Inputs.TryGetValue("target_id",out var id)?id?.ToString():null,request.Inputs.TryGetValue("current_checkpoint",out var cp)?cp?.ToString()??string.Empty:string.Empty);
        return new(request.NarrativeReference,paragraphs,choices,tags,state);
    }

    private static void AssignInputs(Story story,InkRenderRequest request)
    {
        var definitions=request.Package.InkDefinition.Variables.ToDictionary(x=>x.Name,StringComparer.Ordinal);
        foreach(var name in request.Inputs.Keys)
        {
            if(!definitions.TryGetValue(name,out var definition))throw new InkContractException($"Unknown Ink input variable '{name}'.");
            if(request.TargetScope==StoryletScope.WorldPublic&&definition.Visibility==InkVariableVisibility.Private)throw new InkContractException($"Private Ink variable '{name}' cannot enter public rendering.");
        }
        var reference=request.Package.InkDefinition.References.Single(x=>x.Id==request.NarrativeReference);
        foreach(var required in reference.RequiredVariables.Concat(definitions.Values.Where(x=>x.Required).Select(x=>x.Name)).Distinct(StringComparer.Ordinal))
            if(!request.Inputs.TryGetValue(required,out var value)||value is null)throw new InkContractException($"Required Ink input variable '{required}' is missing.");
        foreach(var pair in request.Inputs.OrderBy(x=>x.Key,StringComparer.Ordinal))
        {
            var definition=definitions[pair.Key];var value=ConvertValue(definition,pair.Value);
            try { story.variablesState[pair.Key]=value; }
            catch(Exception ex) { throw new InkContractException($"Ink input variable '{pair.Key}' could not be assigned: {ex.Message}"); }
        }
    }

    private static object ConvertValue(InkInputVariableDefinition definition,object? value)
    {
        if(value is null)throw new InkContractException($"Ink input variable '{definition.Name}' cannot be null.");
        return definition.Type switch
        {
            InkScalarType.String when value is string=>value,
            InkScalarType.Integer when value is int=>value,
            InkScalarType.Integer when value is long and >=int.MinValue and <=int.MaxValue=>(int)(long)value,
            InkScalarType.Decimal when value is decimal=>(float)(decimal)value,
            InkScalarType.Decimal when value is double=>(float)(double)value,
            InkScalarType.Decimal when value is float=>value,
            InkScalarType.Boolean when value is bool=>value,
            _=>throw new InkContractException($"Ink input variable '{definition.Name}' has the wrong scalar type.")
        };
    }

    private static void Continue(Story story,StoryPackage package,List<string> paragraphs,List<InkTag> tags)
    {
        var guard=0;
        while(story.canContinue)
        {
            if(++guard>1000)throw new InkContractException("Ink continuation exceeded the safety limit.");
            var text=story.Continue().Trim();if(text.Length>0)paragraphs.Add(text);
            tags.AddRange(ParseTags(story.currentTags,package));
        }
    }

    private static IReadOnlyList<InkChoice> ReadChoices(Story story,StoryPackage package)
        =>story.currentChoices.Select(x=>new InkChoice(x.index,x.text.Trim(),ParseTags(x.tags,package))).ToList();

    private static IReadOnlyList<InkTag> ParseTags(IEnumerable<string>? raw,StoryPackage package)
    {
        var tags=new List<InkTag>();
        foreach(var item in raw??[])
        {
            var value=item.Trim();var split=value.IndexOf(':');
            var name=(split<0?value:value[..split]).Trim().ToLowerInvariant();var payload=split<0?string.Empty:value[(split+1)..].Trim();
            if(string.IsNullOrWhiteSpace(name)||name.Any(c=>!(char.IsAsciiLetterOrDigit(c)||c is '_' or '-'))||payload.Contains(';')||payload.Contains("&&")||payload.Contains("||")||payload.Contains("$(")||payload.Contains('`'))
                throw new InkContractException($"Dangerous or malformed Ink tag '{value}' was rejected.");
            if(!package.InkDefinition.ApprovedTags.Contains(name,StringComparer.Ordinal)&&!package.InkDefinition.AllowUnknownPresentationTags)
                throw new InkContractException($"Unknown Ink tag '{name}'.");
            if(name=="effect"&&!package.Effects.Any(x=>x.Id==payload))throw new InkContractException($"Unknown Effect tag '{payload}'.");
            tags.Add(new(name,payload));
        }
        return tags;
    }
}

public sealed class InkPackageValidator(IInkStoryLoader loader) : IInkPackageValidator
{
    public async Task<InkValidationResult> ValidateAsync(StoryPackage package,CancellationToken ct=default)
    {
        var errors=new List<InkValidationError>();
        foreach(var duplicate in package.InkDefinition.Variables.GroupBy(x=>x.Name,StringComparer.Ordinal).Where(x=>x.Count()>1))errors.Add(new("duplicate-variable",$"Ink variable '{duplicate.Key}' is duplicated."));
        foreach(var duplicate in package.InkDefinition.References.GroupBy(x=>x.Id,StringComparer.Ordinal).Where(x=>x.Count()>1))errors.Add(new("duplicate-reference",$"Ink reference '{duplicate.Key}' is duplicated.",duplicate.Key));
        foreach(var reference in package.InkDefinition.References.OrderBy(x=>x.Id,StringComparer.Ordinal))
        {
            if(reference.RequiredVariables.Any(v=>package.InkDefinition.Variables.All(x=>x.Name!=v)))errors.Add(new("unknown-required-variable",$"Ink reference requires undeclared variable '{reference.RequiredVariables.First(v=>package.InkDefinition.Variables.All(x=>x.Name!=v))}'.",reference.Id));
            try { _=await loader.LoadAsync(package,reference.Id,reference.Scope,ct); }
            catch(Exception ex) { errors.Add(new("invalid-compiled-story",ex.Message,reference.Id)); }
        }
        return new(errors);
    }
}
