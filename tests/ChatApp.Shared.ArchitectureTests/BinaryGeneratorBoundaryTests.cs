using System.Xml.Linq;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

public sealed class BinaryGeneratorBoundaryTests
{
    [Fact]
    public void BinaryV1HasAUniqueStableIdButRemainsDisabledByDefault()
    {
        Assert.Equal((byte)1, BinaryPayloadFormat.Version);
        Assert.Equal("chatapp-bin-v1", BinaryPayloadFormat.Id);
        Assert.False(ProtocolPayloadFormat.IsValid(BinaryPayloadFormat.Id));
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
    public void RuntimeSchemaPackageOnlyDependsOnCoreAndDoesNotOwnTheGenerator()
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
        string[] referencedProjects = document.Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference"
                && !string.Equals(
                    (string?)element.Attribute("OutputItemType"),
                    "Analyzer",
                    StringComparison.OrdinalIgnoreCase))
            .Select(element => Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(projectPath)!,
                ((string)element.Attribute("Include")!).Replace('\\', Path.DirectorySeparatorChar))))
            .Select(path => Path.GetFileName(path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["ChatApp.Binary.Core.csproj"], referencedProjects);
        Assert.DoesNotContain(document.Descendants(), element =>
            element.Name.LocalName == "ProjectReference"
            && string.Equals(
                (string?)element.Attribute("OutputItemType"),
                "Analyzer",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(string.Equals(
            "true",
            document.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "AllowUnsafeBlocks")
                ?.Value,
            StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NativePointerCodeIsExplicitAndConfinedToCoreCursorIsland()
    {
        string repositoryRoot = FindRepositoryRoot();
        string sourceDirectory = Path.Combine(repositoryRoot, "src");
        string coreDirectory = Path.Combine(
            sourceDirectory,
            "ChatApp.Binary.Core");
        string projectPath = Path.Combine(coreDirectory, "ChatApp.Binary.Core.csproj");
        XDocument document = XDocument.Load(projectPath);

        Assert.Equal("true", GetRequiredProperty(document, "AllowUnsafeBlocks"));

        string[] unsafeProjects = Directory.GetFiles(
                sourceDirectory,
                "*.csproj",
                SearchOption.AllDirectories)
            .Where(path => string.Equals(
                XDocument.Load(path).Descendants()
                    .FirstOrDefault(element => element.Name.LocalName == "AllowUnsafeBlocks")
                    ?.Value,
                "true",
                StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            ["src/ChatApp.Binary.Core/ChatApp.Binary.Core.csproj"],
            unsafeProjects);

        string[] pointerFiles = Directory.GetFiles(
                sourceDirectory,
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("unsafe", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "src/ChatApp.Binary.Core/BinaryCodec.cs",
                "src/ChatApp.Binary.Core/BinaryReadCursor.cs",
                "src/ChatApp.Binary.Core/BinaryWriteCursor.cs"
            ],
            pointerFiles);

        Assert.All(
            Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories),
            sourcePath => Assert.DoesNotContain(
                "System.Runtime.CompilerServices.Unsafe",
                File.ReadAllText(sourcePath),
                StringComparison.Ordinal));
    }

    [Fact]
    public void EncoderOnlyConsumerHasNoGeneratorOrAnalyzerReference()
    {
        string repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(
            repositoryRoot,
            "tests",
            "ChatApp.Binary.EncoderOnly.Tests",
            "ChatApp.Binary.EncoderOnly.Tests.csproj");
        XDocument document = XDocument.Load(projectPath);

        string[] projectReferences = document.Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
            .ToArray();

        Assert.Single(projectReferences);
        Assert.Contains("ChatApp.Binary.Core.csproj", projectReferences[0], StringComparison.Ordinal);
        Assert.DoesNotContain(projectReferences, reference =>
            reference.Contains("Generator", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(document.Descendants(), element =>
            string.Equals(
                (string?)element.Attribute("OutputItemType"),
                "Analyzer",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DiscardedExperimentalRuntimeHasNoSourceSurface()
    {
        string repositoryRoot = FindRepositoryRoot();
        string[] sourceFiles =
        [
            .. Directory.GetFiles(Path.Combine(repositoryRoot, "src"), "*.cs", SearchOption.AllDirectories),
            .. Directory.GetFiles(Path.Combine(repositoryRoot, "generators"), "*.cs", SearchOption.AllDirectories)
        ];
        string[] forbiddenSymbols =
        [
            "TaggedBinary",
            "BinaryFormatProfile",
            "BinaryMeasureCursor",
            "ITcpBinaryCodec",
            "TcpBinaryLimits",
            "TcpBinaryDecodeError"
        ];

        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);
            foreach (string forbiddenSymbol in forbiddenSymbols)
            {
                Assert.DoesNotContain(forbiddenSymbol, source, StringComparison.Ordinal);
            }
        }
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
