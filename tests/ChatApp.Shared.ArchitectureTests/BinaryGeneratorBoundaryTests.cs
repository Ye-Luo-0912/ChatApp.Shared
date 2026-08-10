using System.Xml.Linq;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

public sealed class BinaryGeneratorBoundaryTests
{
    [Fact]
    public void TaggedV1HasAStableIdButRemainsDisabledByDefault()
    {
        Assert.Equal((byte)1, TaggedBinaryPayloadFormat.Version);
        Assert.Equal("chatapp-tagged-v1", TaggedBinaryPayloadFormat.Id);
        Assert.False(ProtocolPayloadFormat.IsValid(TaggedBinaryPayloadFormat.Id));
    }

    [Fact]
    public void GeneratorStaysBuildTimeOnlyAndOutsideRuntimeSourceTree()
    {
        string repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(
            repositoryRoot,
            "generators",
            "ChatApp.Protocol.Tcp.Binary.Generator",
            "ChatApp.Protocol.Tcp.Binary.Generator.csproj");
        XDocument document = XDocument.Load(projectPath);

        Assert.Equal("netstandard2.0", GetRequiredProperty(document, "TargetFramework"));
        Assert.Equal("true", GetRequiredProperty(document, "IsRoslynComponent"));
        Assert.Equal("true", GetRequiredProperty(document, "DevelopmentDependency"));
        Assert.Equal("false", GetRequiredProperty(document, "IncludeBuildOutput"));
        Assert.Equal("true", GetRequiredProperty(document, "SuppressDependenciesWhenPacking"));

        XElement[] packageReferences = document.Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .ToArray();
        XElement packageReference = Assert.Single(packageReferences);
        Assert.Equal("Microsoft.CodeAnalysis.CSharp", (string?)packageReference.Attribute("Include"));
        Assert.Equal("all", (string?)packageReference.Attribute("PrivateAssets"));

        Assert.DoesNotContain(document.Descendants(), element =>
            element.Name.LocalName is "ProjectReference" or "FrameworkReference" or "Reference");
    }

    [Fact]
    public void RuntimeAdapterOnlyDependsOnCanonicalTcpContracts()
    {
        string repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(
            repositoryRoot,
            "src",
            "ChatApp.Protocol.Tcp.Binary",
            "ChatApp.Protocol.Tcp.Binary.csproj");
        XDocument document = XDocument.Load(projectPath);

        Assert.DoesNotContain(document.Descendants(), element =>
            element.Name.LocalName is "PackageReference" or "FrameworkReference" or "Reference");
        XElement projectReference = Assert.Single(
            document.Descendants(),
            element => element.Name.LocalName == "ProjectReference");
        var referencedProjectPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(projectPath)!,
            ((string)projectReference.Attribute("Include")!).Replace('\\', Path.DirectorySeparatorChar)));
        var referencedProject = new FileInfo(referencedProjectPath);
        Assert.Equal("ChatApp.Protocol.Tcp.csproj", referencedProject.Name,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal("ChatApp.Protocol.Tcp", referencedProject.Directory?.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void NativePointerCodeIsExplicitAndConfinedToOneFixedWidthHelper()
    {
        string repositoryRoot = FindRepositoryRoot();
        string runtimeDirectory = Path.Combine(
            repositoryRoot,
            "src",
            "ChatApp.Protocol.Tcp.Binary");
        string projectPath = Path.Combine(runtimeDirectory, "ChatApp.Protocol.Tcp.Binary.csproj");
        XDocument document = XDocument.Load(projectPath);

        Assert.Equal("true", GetRequiredProperty(document, "AllowUnsafeBlocks"));

        string[] pointerFiles = Directory.GetFiles(
                runtimeDirectory,
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("unsafe", StringComparison.Ordinal))
            .Select(path => Path.GetFileName(path)!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["NativeFixedWidth.cs"], pointerFiles);

        string nativeSource = File.ReadAllText(Path.Combine(runtimeDirectory, "NativeFixedWidth.cs"));
        Assert.DoesNotContain("System.Runtime.CompilerServices.Unsafe", nativeSource, StringComparison.Ordinal);
    }

    private static string GetRequiredProperty(XDocument document, string propertyName) =>
        document.Descendants()
            .First(element => element.Name.LocalName == propertyName)
            .Value;

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
}
