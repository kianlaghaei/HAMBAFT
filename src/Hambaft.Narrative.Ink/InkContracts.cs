using Hambaft.Domain;

namespace Hambaft.Narrative.Ink;

public sealed record InkStoryRootOptions(string Root);
public sealed record InkTag(string Name,string Value);
public sealed record InkChoice(int Index,string Text,IReadOnlyList<InkTag> Tags);
public sealed record InkRenderRequest(
    StoryPackage Package,
    string NarrativeReference,
    StoryletScope TargetScope,
    IReadOnlyDictionary<string,object?> Inputs,
    int? ChoiceIndex = null,
    InkStateEnvelope? State = null);
public sealed record InkRenderedNarrative(
    string NarrativeReference,
    IReadOnlyList<string> Paragraphs,
    IReadOnlyList<InkChoice> Choices,
    IReadOnlyList<InkTag> Tags,
    InkStateEnvelope State);
public sealed record InkStateEnvelope(
    string PackageId,string PackageVersion,string ContentHash,string NarrativeReference,
    StoryletScope TargetScope,string? TargetId,string Checkpoint,string RuntimeStateJson);
public sealed record InkValidationError(string Code,string Message,string? NarrativeReference=null);
public sealed record InkValidationResult(IReadOnlyList<InkValidationError> Errors)
{
    public bool IsValid=>Errors.Count==0;
}

public sealed class InkStoryHandle
{
    internal global::Ink.Runtime.Story Runtime { get; }
    public string PackageId { get; }
    public string PackageVersion { get; }
    public string ContentHash { get; }
    public string NarrativeReference { get; }
    public StoryletScope TargetScope { get; }
    internal InkStoryHandle(global::Ink.Runtime.Story runtime,StoryPackage package,string narrativeReference,StoryletScope scope)
    { Runtime=runtime;PackageId=package.Manifest.Id;PackageVersion=package.Manifest.Version;ContentHash=package.ContentHash;NarrativeReference=narrativeReference;TargetScope=scope; }
}

public interface IInkStoryLoader
{
    Task<InkStoryHandle> LoadAsync(StoryPackage package,string narrativeReference,StoryletScope targetScope,CancellationToken ct=default);
}
public interface IInkNarrativeRenderer
{
    Task<InkRenderedNarrative> RenderAsync(InkRenderRequest request,CancellationToken ct=default);
}
public interface IInkStateSerializer
{
    InkStateEnvelope Serialize(InkStoryHandle story,string? targetId,string checkpoint);
    void Restore(InkStoryHandle story,InkStateEnvelope state);
}
public interface IInkPackageValidator
{
    Task<InkValidationResult> ValidateAsync(StoryPackage package,CancellationToken ct=default);
}

public sealed class InkContractException(string message) : Exception(message);
