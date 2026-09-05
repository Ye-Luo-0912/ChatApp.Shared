using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary;
using ChatApp.Shared.Protocol.Tcp.Binary.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Generator.Tests;

public sealed class TcpBinaryCodecGeneratorTests
{
    [Theory]
    [MemberData(nameof(InvalidShapeCases))]
    public void InvalidSymbolShapesProduceOwnedDiagnosticsInsteadOfBrokenSources(
        string source,
        string expectedDiagnostic)
    {
        GeneratorDriverRunResult result = RunGenerator(source, out Compilation output);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == expectedDiagnostic);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "CS8785");
        Assert.DoesNotContain(
            output.GetDiagnostics(),
            diagnostic => diagnostic.Id == "CS8785");
    }

    [Fact]
    public void PreviouslyCollidingDescriptorNamesGetDistinctStableHintNames()
    {
        const string source = """
            namespace A.B_C
            {
                using ChatApp.Shared.Protocol.Tcp.Binary.Generation;
                internal sealed class Contract { public int Id { get; set; } }
                [TcpBinaryContract(typeof(Contract))]
                [TcpBinaryField(1, nameof(Contract.Id))]
                internal static partial class Descriptor;
            }

            namespace A_B.C
            {
                using ChatApp.Shared.Protocol.Tcp.Binary.Generation;
                internal sealed class Contract { public int Id { get; set; } }
                [TcpBinaryContract(typeof(Contract))]
                [TcpBinaryField(1, nameof(Contract.Id))]
                internal static partial class Descriptor;
            }
            """;

        GeneratorDriverRunResult result = RunGenerator(source, out Compilation output);
        GeneratorRunResult generator = Assert.Single(result.Results);
        string[] codecHints = generator.GeneratedSources
            .Select(generated => generated.HintName)
            .Where(hint => hint.StartsWith("TcpBinaryCodec_", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(2, codecHints.Length);
        Assert.Equal(2, codecHints.Distinct(StringComparer.Ordinal).Count());
        GeneratedSourceResult attributeSource = Assert.Single(
            generator.GeneratedSources,
            generated => generated.HintName == "TcpBinaryDescriptorAttributes.g.cs");
        string attributes = attributeSource.SourceText.ToString();
        Assert.Contains("internal sealed class TcpBinaryContractAttribute", attributes, StringComparison.Ordinal);
        Assert.Contains("internal sealed class TcpBinaryFieldAttribute", attributes, StringComparison.Ordinal);
        Assert.DoesNotContain("public sealed class TcpBinary", attributes, StringComparison.Ordinal);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(output.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    public static TheoryData<string, string> InvalidShapeCases => new()
    {
        {
            """
            using ChatApp.Shared.Protocol.Tcp.Binary.Generation;
            internal sealed class Contract { public int Id { get; set; } }
            [TcpBinaryContract(typeof(Contract))]
            [TcpBinaryField(1, nameof(Contract.Id))]
            internal static partial class Descriptor<T>;
            """,
            "CTB001"
        },
        {
            """
            using ChatApp.Shared.Protocol.Tcp.Binary.Generation;
            internal sealed class Contract { public int Id { get; set; } }
            [TcpBinaryContract(typeof(Contract))]
            [TcpBinaryField(1, nameof(Contract.Id))]
            file static partial class Descriptor;
            """,
            "CTB001"
        },
        {
            """
            using ChatApp.Shared.Protocol.Tcp.Binary.Generation;
            internal class BaseContract { }
            internal sealed class Contract : BaseContract { public int Id { get; set; } }
            [TcpBinaryContract(typeof(Contract))]
            [TcpBinaryField(1, nameof(Contract.Id))]
            internal static partial class Descriptor;
            """,
            "CTB002"
        },
        {
            """
            using ChatApp.Shared.Protocol.Tcp.Binary.Generation;
            internal sealed class Contract<T> { public T? Value { get; set; } }
            [TcpBinaryContract(typeof(Contract<>))]
            [TcpBinaryField(1, "Value")]
            internal static partial class Descriptor;
            """,
            "CTB002"
        },
        {
            """
            using ChatApp.Shared.Protocol.Tcp.Binary.Generation;
            file sealed class Contract { public int Id { get; set; } }
            [TcpBinaryContract(typeof(Contract))]
            [TcpBinaryField(1, nameof(Contract.Id))]
            internal static partial class Descriptor;
            """,
            "CTB002"
        },
        {
            """
            using ChatApp.Shared.Protocol.Tcp.Binary.Generation;
            internal sealed class Contract
            {
                public int this[int index] { get => index; set { } }
            }
            [TcpBinaryContract(typeof(Contract))]
            [TcpBinaryField(1, "this[]")]
            internal static partial class Descriptor;
            """,
            "CTB004"
        }
    };

    private static GeneratorDriverRunResult RunGenerator(string source, out Compilation output)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        MetadataReference[] references = GetMetadataReferences();
        CSharpCompilation input = CSharpCompilation.Create(
            $"GeneratorFixture_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TcpBinaryCodecGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(input, out output, out _);
        return driver.GetRunResult();
    }

    private static MetadataReference[] GetMetadataReferences()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        IEnumerable<string> paths = trustedAssemblies.Split(Path.PathSeparator)
            .Append(typeof(ITcpBinaryCodec<>).Assembly.Location)
            .Append(typeof(ProtocolPayloadFormat).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return paths.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
    }
}
