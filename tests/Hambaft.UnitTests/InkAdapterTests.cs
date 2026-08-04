using FluentAssertions;
using Hambaft.Domain;
using Hambaft.Infrastructure.StoryPackages;
using Hambaft.Narrative.Ink;

namespace Hambaft.UnitTests;

public sealed class InkAdapterTests
{
    [Fact]
    public async Task Compiled_story_renders_public_and_private_text_deterministically()
    {
        var x=await Create();
        var publicResult=await x.Renderer.RenderAsync(new(x.Package,"a-public",StoryletScope.WorldPublic,new Dictionary<string,object?>()));
        var request=new InkRenderRequest(x.Package,"a-bakery",StoryletScope.TeamPrivate,new Dictionary<string,object?>{{"entity_display_name","نانوایی سپیده"}});
        var first=await x.Renderer.RenderAsync(request);var second=await x.Renderer.RenderAsync(request);
        publicResult.Paragraphs.Should().Contain(x=>x.Contains("زنگ بازار"));
        first.Paragraphs.Should().Contain(x=>x.Contains("نانوایی سپیده"));
        first.Paragraphs.Should().Equal(second.Paragraphs);
        first.Tags.Should().Contain(x=>x.Name=="speaker"&&x.Value=="رحیم");
    }

    [Fact]
    public async Task Inputs_are_allowlisted_typed_required_and_private_safe()
    {
        var x=await Create();
        await FluentActions.Awaiting(()=>x.Renderer.RenderAsync(new(x.Package,"a-bakery",StoryletScope.TeamPrivate,new Dictionary<string,object?>()))).Should().ThrowAsync<InkContractException>().WithMessage("*required*");
        await FluentActions.Awaiting(()=>x.Renderer.RenderAsync(new(x.Package,"a-bakery",StoryletScope.TeamPrivate,new Dictionary<string,object?>{{"entity_display_name",42}}))).Should().ThrowAsync<InkContractException>().WithMessage("*wrong scalar type*");
        await FluentActions.Awaiting(()=>x.Renderer.RenderAsync(new(x.Package,"a-public",StoryletScope.WorldPublic,new Dictionary<string,object?>{{"unknown",true}}))).Should().ThrowAsync<InkContractException>().WithMessage("Unknown Ink input*");
        await FluentActions.Awaiting(()=>x.Renderer.RenderAsync(new(x.Package,"a-public",StoryletScope.WorldPublic,new Dictionary<string,object?>{{"team_display_name","محرمانه"}}))).Should().ThrowAsync<InkContractException>().WithMessage("Private Ink variable*");
    }

    [Fact]
    public async Task Choices_tags_effect_validation_and_entry_validation_are_enforced()
    {
        var x=await Create();
        var initial=await x.Renderer.RenderAsync(new(x.Package,"adapter-test",StoryletScope.TeamPrivate,new Dictionary<string,object?>{{"entity_display_name","سپیده"}}));
        initial.Choices.Should().HaveCount(2);
        var selected=await x.Renderer.RenderAsync(new(x.Package,"adapter-test",StoryletScope.TeamPrivate,new Dictionary<string,object?>{{"entity_display_name","سپیده"}},1));
        selected.Paragraphs.Should().Contain(x=>x.Contains("انتخاب دوم"));
        var noEffects=x.Package with { Effects=[] };
        await FluentActions.Awaiting(()=>x.Renderer.RenderAsync(new(noEffects,"adapter-effect-test",StoryletScope.TeamPrivate,new Dictionary<string,object?>()))).Should().ThrowAsync<InkContractException>().WithMessage("Unknown Effect tag*");
        var badReference=x.Package.InkDefinition.References.Single(r=>r.Id=="adapter-test") with { EntryPoint="missing_knot" };
        var badInk=x.Package.InkDefinition with { References=[badReference] };var badPackage=x.Package with { Ink=badInk };
        await FluentActions.Awaiting(()=>x.Loader.LoadAsync(badPackage,"adapter-test",StoryletScope.TeamPrivate)).Should().ThrowAsync<InkContractException>().WithMessage("Ink entry point*");
    }

    [Fact]
    public async Task State_round_trip_is_stable_and_hash_mismatch_is_rejected()
    {
        var x=await Create();var handle=await x.Loader.LoadAsync(x.Package,"adapter-test",StoryletScope.TeamPrivate);
        var state=x.Serializer.Serialize(handle,"entity-1","morning-without-bell");
        var restored=await x.Renderer.RenderAsync(new(x.Package,"adapter-test",StoryletScope.TeamPrivate,new Dictionary<string,object?>{{"entity_display_name","سپیده"}},State:state));
        restored.Choices.Should().HaveCount(2);
        var bad=state with { ContentHash="sha256:changed" };
        await FluentActions.Awaiting(()=>x.Renderer.RenderAsync(new(x.Package,"adapter-test",StoryletScope.TeamPrivate,new Dictionary<string,object?>{{"entity_display_name","سپیده"}},State:bad))).Should().ThrowAsync<InkContractException>().WithMessage("*locked package hash*");
    }

    [Fact]
    public async Task Hezar_Cheragh_package_and_every_compiled_entry_point_validate()
    {
        var x=await Create();var validation=await new InkPackageValidator(x.Loader).ValidateAsync(x.Package);
        validation.Errors.Should().BeEmpty();
        x.Package.ContentHash.Should().StartWith("sha256:");
        x.Package.EntityEndingDefinitions.Should().HaveCount(12);
        x.Package.WorldEndingDefinitions.Should().HaveCount(5);
    }

    private static async Task<Fixture> Create()
    {
        var root=RepositoryRoot();var stories=Path.Combine(root,"stories");var options=new StoryPackageOptions{Root=stories};
        var inkLoader=new FileInkStoryLoader(new(stories));var serializer=new InkStateSerializer();
        var packageLoader=new FileSystemStoryPackageLoader(options,new DeterministicStoryPackageHasher(),new StoryPackageValidator());
        var package=await packageLoader.LoadAsync("hezar-cheragh","0.1.0",CancellationToken.None);
        return new(package,inkLoader,serializer,new InkNarrativeRenderer(inkLoader,serializer));
    }
    private static string RepositoryRoot(){var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"Hambaft.sln")))directory=directory.Parent;return directory?.FullName??throw new DirectoryNotFoundException();}
    private sealed record Fixture(StoryPackage Package,IInkStoryLoader Loader,IInkStateSerializer Serializer,IInkNarrativeRenderer Renderer);
}
