using System.Text.Json;
using FluentAssertions;
using Hambaft.Domain;

namespace Hambaft.UnitTests;

public sealed class StoryPackageTests
{
    [Fact] public async Task Valid_package_loads(){var s=StoryRuntimeTestSupport.PackageServices();var p=await s.Loader.LoadAsync("sample-cargo-delay","1.0.0",default);p.Manifest.Id.Should().Be("sample-cargo-delay");p.ContentHash.Should().StartWith("sha256:");}
    [Fact] public async Task Duplicate_ids_fail(){var(s,p)=await Package();var duplicate=p with { Metrics=p.Metrics.Append(p.Metrics[0]).ToList() };s.Validate(duplicate).Errors.Should().Contain(x=>x.ErrorCode=="duplicate-metric-id");}
    [Fact] public async Task Missing_narrative_reference_fails(){var(s,p)=await Package();var changed=p with { Storylets=p.Storylets.Select((x,i)=>i==0?x with { NarrativeRef="missing" }:x).ToList() };s.Validate(changed).Errors.Should().Contain(x=>x.ErrorCode=="missing-narrative-reference");}
    [Fact] public async Task Unknown_effect_fails(){var(s,p)=await Package();var story=p.Storylets[1];var choice=story.Choices[0] with { EffectIds=["missing"] };var changed=p with { Storylets=p.Storylets.Select(x=>x.Id==story.Id?x with { Choices=[choice,..story.Choices.Skip(1)] }:x).ToList() };s.Validate(changed).Errors.Should().Contain(x=>x.ErrorCode=="unknown-effect");}
    [Fact] public async Task Unknown_metric_fails(){var(s,p)=await Package();var effect=p.Effects[0] with { MetricKey="missing" };var changed=p with { Effects=p.Effects.Select((x,i)=>i==0?effect:x).ToList() };s.Validate(changed).Errors.Should().Contain(x=>x.ErrorCode=="unknown-metric");}
    [Fact] public async Task Invalid_choice_reference_fails(){var(s,p)=await Package();var story=p.Storylets[1];var choice=story.Choices[0] with { LabelRef="missing" };var changed=p with { Storylets=p.Storylets.Select(x=>x.Id==story.Id?x with { Choices=[choice,..story.Choices.Skip(1)] }:x).ToList() };s.Validate(changed).Errors.Should().Contain(x=>x.ErrorCode=="invalid-choice-reference");}
    [Fact] public async Task Invalid_checkpoint_fails(){var(s,p)=await Package();var changed=p with { Manifest=p.Manifest with { EntryCheckpointId="missing" } };s.Validate(changed).Errors.Should().Contain(x=>x.ErrorCode=="missing-entry-checkpoint");}
    [Fact] public async Task Canonical_hash_ignores_json_formatting()
    {
        var services=StoryRuntimeTestSupport.PackageServices();var source=Path.Combine(StoryRuntimeTestSupport.StoriesRoot,"sample-cargo-delay","1.0.0");var temp=Path.Combine(Path.GetTempPath(),"hambaft-hash-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        try { foreach(var file in Directory.GetFiles(source)){var target=Path.Combine(temp,Path.GetFileName(file));using var json=JsonDocument.Parse(await File.ReadAllTextAsync(file));if(Path.GetFileName(file)=="manifest.json"){var reversed=json.RootElement.EnumerateObject().Reverse().ToDictionary(x=>x.Name,x=>x.Value.Clone());await File.WriteAllTextAsync(target,JsonSerializer.Serialize(reversed,new JsonSerializerOptions{WriteIndented=true}));}else await File.WriteAllTextAsync(target,JsonSerializer.Serialize(json.RootElement,new JsonSerializerOptions{WriteIndented=true}));}var a=await services.Hasher.ComputeAsync(source,default);var b=await services.Hasher.ComputeAsync(temp,default);b.Should().Be(a); }
        finally { Directory.Delete(temp,true); }
    }
    [Fact] public async Task Semantic_change_changes_hash()
    {
        var services=StoryRuntimeTestSupport.PackageServices();var source=Path.Combine(StoryRuntimeTestSupport.StoriesRoot,"sample-cargo-delay","1.0.0");var temp=Path.Combine(Path.GetTempPath(),"hambaft-hash-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        try { foreach(var file in Directory.GetFiles(source))File.Copy(file,Path.Combine(temp,Path.GetFileName(file)));var path=Path.Combine(temp,"manifest.json");await File.WriteAllTextAsync(path,(await File.ReadAllTextAsync(path)).Replace("10,","11,"));(await services.Hasher.ComputeAsync(temp,default)).Should().NotBe(await services.Hasher.ComputeAsync(source,default)); }
        finally { Directory.Delete(temp,true); }
    }
    private static async Task<(Hambaft.Application.IStoryPackageValidator,StoryPackage)> Package(){var s=StoryRuntimeTestSupport.PackageServices();return(s.Validator,await s.Loader.LoadAsync("sample-cargo-delay","1.0.0",default));}
}
