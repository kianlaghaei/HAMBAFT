using Hambaft.Domain;

namespace Hambaft.Application;

public interface IStoryPackageCatalog
{
    Task<IReadOnlyList<StoryPackageMetadata>> ListAsync(CancellationToken cancellationToken);
}

public interface IStoryPackageLoader
{
    Task<StoryPackage> LoadAsync(string packageId,string version,CancellationToken cancellationToken);
}

public sealed record StoryPackageAsset(byte[] Content,string ContentType);

public interface IStoryPackageAssetReader
{
    Task<StoryPackageAsset> ReadAsync(string packageId,string version,string assetId,CancellationToken cancellationToken);
}

public interface IStoryPackageValidator
{
    StoryPackageValidationResult Validate(StoryPackage package);
}

public interface IStoryPackageHasher
{
    Task<string> ComputeAsync(string packageDirectory,CancellationToken cancellationToken);
}

public sealed class StoryPackageNotFoundException(string packageId,string version)
    : Exception($"Story Package '{packageId}' version '{version}' was not found.");

public sealed class InvalidStoryPackageException(IReadOnlyList<StoryPackageValidationError> errors)
    : Exception($"Story Package validation failed with {errors.Count} error(s).")
{
    public IReadOnlyList<StoryPackageValidationError> Errors { get; }=errors;
}

public sealed class StoryPackageHashMismatchException(string expected,string actual)
    : Exception($"Story Package content hash mismatch. Expected '{expected}', actual '{actual}'.")
{
    public string Expected { get; }=expected;
    public string Actual { get; }=actual;
}
