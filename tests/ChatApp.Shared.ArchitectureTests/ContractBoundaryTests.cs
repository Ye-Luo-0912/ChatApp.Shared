using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

public sealed class ContractBoundaryTests
{
    private static readonly HashSet<string> NonPackableMarkerProjects =
        new(StringComparer.Ordinal);

    private static readonly string[] ForbiddenAssemblyPrefixes =
    [
        "Confluent.Kafka",
        "Dapper",
        "Grpc",
        "MassTransit",
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "NATS",
        "Newtonsoft.Json",
        "Npgsql",
        "Serilog",
        "StackExchange.Redis"
    ];

    [Fact]
    public void ContractProjectsHaveUniqueExplicitIdentitiesAndTargetNet10()
    {
        ProjectDescriptor[] projects = DiscoverContractProjects();

        Assert.Equal(6, projects.Length);
        Assert.All(projects, project => Assert.Equal("net10.0", project.TargetFramework));
        Assert.All(projects, project => Assert.False(string.IsNullOrWhiteSpace(project.PackageId)));
        Assert.All(projects, project => Assert.False(string.IsNullOrWhiteSpace(project.AssemblyName)));
        Assert.All(projects, project => Assert.False(string.IsNullOrWhiteSpace(project.RootNamespace)));
        Assert.Equal(projects.Length, projects.Select(project => project.PackageId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(projects.Length, projects.Select(project => project.AssemblyName).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(projects.Length, projects.Select(project => project.RootNamespace).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ReleaseVersionAndPackableProjectSetAreExplicit()
    {
        string repositoryRoot = FindRepositoryRoot();
        string buildPropsPath = Path.Combine(repositoryRoot, "Directory.Build.props");
        XDocument buildProps = XDocument.Load(buildPropsPath);

        ProjectDescriptor[] projects = DiscoverContractProjects();
        Assert.Equal("0.5.1", GetRequiredProperty(buildProps, "VersionPrefix", buildPropsPath));
        Assert.All(
            projects,
            project => Assert.True(
                string.IsNullOrWhiteSpace(GetOptionalProperty(project.Document, "VersionPrefix")),
                $"{project.Path} must inherit the unified repository release version."));

        string[] actualNonPackableProjects = projects
            .Where(project => string.Equals(
                GetOptionalProperty(project.Document, "IsPackable"),
                "false",
                StringComparison.OrdinalIgnoreCase))
            .Select(project => project.PackageId)
            .OrderBy(packageId => packageId, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            NonPackableMarkerProjects.OrderBy(packageId => packageId, StringComparer.Ordinal),
            actualNonPackableProjects);

        Assert.All(
            projects.Where(project => !NonPackableMarkerProjects.Contains(project.PackageId)),
            project =>
            {
                Assert.False(string.IsNullOrWhiteSpace(GetOptionalProperty(project.Document, "Title")));
                Assert.False(string.IsNullOrWhiteSpace(GetOptionalProperty(project.Document, "Description")));
                Assert.False(string.IsNullOrWhiteSpace(GetOptionalProperty(project.Document, "PackageTags")));
            });
    }

    [Fact]
    public void ContractProjectsHaveNoExternalPackageFrameworkOrAssemblyReferences()
    {
        foreach (ProjectDescriptor project in DiscoverContractProjects())
        {
            string[] forbiddenItems = project.Document
                .Descendants()
                .Where(element => element.Name.LocalName is "PackageReference" or "FrameworkReference" or "Reference")
                .Select(element => $"{element.Name.LocalName}:{(string?)element.Attribute("Include") ?? element.Value}")
                .ToArray();

            Assert.True(
                forbiddenItems.Length == 0,
                $"{project.Path} must remain BCL-only but contains {string.Join(", ", forbiddenItems)}.");
        }
    }

    [Fact]
    public void ContractProjectReferencesStayInsideTheSharedSourceTree()
    {
        string sourceRoot = Path.GetFullPath(Path.Combine(FindRepositoryRoot(), "src"));
        string sourceRootPrefix = sourceRoot + Path.DirectorySeparatorChar;

        foreach (ProjectDescriptor project in DiscoverContractProjects())
        {
            string projectDirectory = Path.GetDirectoryName(project.Path)!;
            IEnumerable<string> references = project.Document
                .Descendants()
                .Where(element => element.Name.LocalName == "ProjectReference")
                .Select(element => (string?)element.Attribute("Include"))
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include!)));

            foreach (string reference in references)
            {
                Assert.True(
                    reference.StartsWith(sourceRootPrefix, StringComparison.OrdinalIgnoreCase),
                    $"{project.Path} references a project outside the shared source tree: {reference}.");
            }
        }
    }

    [Fact]
    public void CompiledContractAssembliesOnlyReferenceBclOrOtherSharedAssemblies()
    {
        foreach (ProjectDescriptor project in DiscoverContractProjects())
        {
            string assemblyPath = Path.Combine(AppContext.BaseDirectory, project.AssemblyName + ".dll");
            Assert.True(File.Exists(assemblyPath), $"Expected contract output was not copied to the test directory: {assemblyPath}.");

            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            string[] references = assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name ?? string.Empty)
                .ToArray();

            string[] forbiddenReferences = references
                .Where(reference => ForbiddenAssemblyPrefixes.Any(prefix =>
                    reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            string[] nonBclReferences = references
                .Where(reference => !IsAllowedBclOrSharedAssembly(reference))
                .ToArray();

            Assert.True(
                forbiddenReferences.Length == 0,
                $"{project.AssemblyName} references forbidden assemblies: {string.Join(", ", forbiddenReferences)}.");
            Assert.True(
                nonBclReferences.Length == 0,
                $"{project.AssemblyName} references non-BCL assemblies: {string.Join(", ", nonBclReferences)}.");
        }
    }

    private static bool IsAllowedBclOrSharedAssembly(string assemblyName) =>
        assemblyName is "mscorlib" or "netstandard" or "Microsoft.CSharp" or "WindowsBase"
        || assemblyName.StartsWith("System", StringComparison.Ordinal)
        || assemblyName.StartsWith("ChatApp.", StringComparison.Ordinal);

    private static ProjectDescriptor[] DiscoverContractProjects()
    {
        string sourceRoot = Path.Combine(FindRepositoryRoot(), "src");

        return Directory.GetFiles(sourceRoot, "*.csproj", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path =>
            {
                XDocument document = XDocument.Load(path);
                return new ProjectDescriptor(
                    path,
                    document,
                    GetRequiredProperty(document, "TargetFramework", path),
                    GetRequiredProperty(document, "PackageId", path),
                    GetRequiredProperty(document, "AssemblyName", path),
                    GetRequiredProperty(document, "RootNamespace", path));
            })
            .ToArray();
    }

    private static string GetRequiredProperty(XDocument document, string propertyName, string projectPath)
    {
        string? value = document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == propertyName)
            ?.Value;

        Assert.False(string.IsNullOrWhiteSpace(value), $"{projectPath} must explicitly declare {propertyName}.");
        return value!;
    }

    private static string? GetOptionalProperty(XDocument document, string propertyName) =>
        document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == propertyName)
            ?.Value;

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ChatApp.Shared.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the ChatApp.Shared repository root.");
    }

    private sealed record ProjectDescriptor(
        string Path,
        XDocument Document,
        string TargetFramework,
        string PackageId,
        string AssemblyName,
        string RootNamespace);
}
