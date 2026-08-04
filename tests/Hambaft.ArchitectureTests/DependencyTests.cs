using FluentAssertions;
using Hambaft.Domain;

namespace Hambaft.ArchitectureTests;

public sealed class DependencyTests
{
    [Fact] public void Domain_has_no_production_dependency() => ProductionReferences(typeof(StorySession).Assembly).Should().BeEmpty();
    [Fact] public void Contracts_has_no_domain_or_infrastructure_dependency() => ProductionReferences(typeof(Hambaft.Contracts.ContractAssemblyMarker).Assembly).Should().BeEmpty();
    [Fact] public void Application_does_not_reference_api() => Names(typeof(Hambaft.Application.ApplicationAssemblyMarker).Assembly).Should().NotContain("Hambaft.Api");
    [Fact] public void Infrastructure_references_only_lower_layers() => ProductionReferences(typeof(Hambaft.Infrastructure.InfrastructureAssemblyMarker).Assembly).Should().BeSubsetOf(["Hambaft.Application","Hambaft.Domain"]);
    [Fact]
    public void Api_is_the_composition_root()
    {
        var project=FindRepositoryRoot().Combine("src","Hambaft.Api","Hambaft.Api.csproj");
        var references=System.Xml.Linq.XDocument.Load(project).Descendants("ProjectReference").Select(x=>Path.GetFileNameWithoutExtension((string)x.Attribute("Include")!));
        references.Should().BeEquivalentTo(["Hambaft.Application","Hambaft.Contracts","Hambaft.Infrastructure"]);
    }

    [Fact]
    public void Production_projects_never_reference_sharedworld()
    {
        var assemblies=new[]{typeof(StorySession).Assembly,typeof(Hambaft.Application.ApplicationAssemblyMarker).Assembly,typeof(Hambaft.Contracts.ContractAssemblyMarker).Assembly,typeof(Hambaft.Infrastructure.InfrastructureAssemblyMarker).Assembly,typeof(Program).Assembly};
        assemblies.SelectMany(Names).Should().NotContain(x=>x.Contains("SharedWorld",StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Forbidden_legacy_symbols_are_absent()
    {
        var forbidden=new[]{"Country","Participant","TeamMember","Questionnaire","PolicyProfile","DecisionSeal","PolicyPackage","EconomicRound","CountryReport","EconomicFeatureVector"};
        typeof(StorySession).Assembly.GetExportedTypes().Select(x=>x.Name).Should().NotContain(x=>forbidden.Contains(x,StringComparer.Ordinal));
    }

    private static IEnumerable<string> ProductionReferences(System.Reflection.Assembly assembly)=>Names(assembly).Where(x=>x.StartsWith("Hambaft.",StringComparison.Ordinal) && x!="Hambaft.Api").Where(x=>x!=assembly.GetName().Name);
    private static IEnumerable<string> Names(System.Reflection.Assembly assembly)=>assembly.GetReferencedAssemblies().Select(x=>x.Name!).Where(x=>x is not null);
    private static DirectoryInfo FindRepositoryRoot(){var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"Hambaft.sln")))directory=directory.Parent;return directory??throw new DirectoryNotFoundException("Hambaft.sln not found.");}
}

internal static class PathExtensions { public static string Combine(this DirectoryInfo root,params string[] parts)=>Path.Combine([root.FullName,..parts]); }
