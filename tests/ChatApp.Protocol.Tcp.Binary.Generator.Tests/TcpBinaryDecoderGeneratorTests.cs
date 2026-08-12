using System.Buffers;
using System.Reflection;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp.Binary.Generation;
using ChatApp.Shared.Protocol.Tcp.Binary.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Generator.Tests;

public sealed class TcpBinaryDecoderGeneratorTests
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
    public void PreviouslyCollidingDescriptorNamesGetDistinctStableDecoderHintNames()
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
        string[] decoderHints = generator.GeneratedSources
            .Select(generated => generated.HintName)
            .Where(hint => hint.StartsWith("TcpBinaryDecoder_", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(2, decoderHints.Length);
        Assert.Equal(2, decoderHints.Distinct(StringComparer.Ordinal).Count());
        Assert.All(decoderHints, hint => Assert.True(hint.Length <= 96, hint));
        Assert.DoesNotContain(
            generator.GeneratedSources,
            generated => generated.HintName == "TcpBinaryDescriptorAttributes.g.cs");
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(output.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void GeneratedDescriptorIsDecodeOnlyAndDoesNotReferenceEncodingInfrastructure()
    {
        const string source = """
            using ChatApp.Shared.Protocol.Tcp.Binary.Generation;
            internal sealed class Contract
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
                public byte[] Data { get; set; } = [];
                public required int RequiredId { get; set; }
            }

            [TcpBinaryContract(typeof(Contract))]
            [TcpBinaryField(1, nameof(Contract.Id))]
            [TcpBinaryField(2, nameof(Contract.Name))]
            [TcpBinaryField(3, nameof(Contract.Data))]
            [TcpBinaryField(4, nameof(Contract.RequiredId))]
            internal static partial class Descriptor;
            """;

        GeneratorDriverRunResult result = RunGenerator(source, out Compilation output);
        GeneratorRunResult generator = Assert.Single(result.Results);
        GeneratedSourceResult generated = Assert.Single(
            generator.GeneratedSources,
            candidate => candidate.HintName.StartsWith("TcpBinaryDecoder_", StringComparison.Ordinal));
        string decoder = generated.SourceText.ToString();

        Assert.Contains("internal static global::ChatApp.Binary.Core.BinaryStatus TryDecode(", decoder,
            StringComparison.Ordinal);
        Assert.Contains("global::System.ReadOnlySpan<byte> payload", decoder, StringComparison.Ordinal);
        Assert.Contains("in global::System.Buffers.ReadOnlySequence<byte> payload", decoder,
            StringComparison.Ordinal);
        Assert.Contains("if (payload.IsSingleSegment)", decoder, StringComparison.Ordinal);
        Assert.Contains("payload.FirstSpan, limits, out value", decoder, StringComparison.Ordinal);
        Assert.Contains("BinaryCodec.TryDecode<ContiguousDecoder,", decoder, StringComparison.Ordinal);
        Assert.Contains("BinaryCodec.TryDecode<SequenceDecoder,", decoder, StringComparison.Ordinal);
        Assert.Contains("private readonly struct ContiguousDecoder", decoder, StringComparison.Ordinal);
        Assert.Contains("IBinaryDecoder<ContiguousDecoder,", decoder, StringComparison.Ordinal);
        Assert.Contains("private readonly struct SequenceDecoder", decoder, StringComparison.Ordinal);
        Assert.Contains("IBinarySequenceDecoder<SequenceDecoder,", decoder, StringComparison.Ordinal);
        Assert.Contains("ref global::ChatApp.Binary.Core.BinaryReadCursor reader", decoder,
            StringComparison.Ordinal);
        Assert.Contains("ref global::ChatApp.Binary.Core.BinarySequenceReadCursor reader", decoder,
            StringComparison.Ordinal);
        Assert.Contains("global::System.ReadOnlySpan<byte> field1 = default;", decoder,
            StringComparison.Ordinal);
        Assert.Contains("global::System.Buffers.ReadOnlySequence<byte> field1 = default;", decoder,
            StringComparison.Ordinal);
        Assert.Contains("reader.TryReadUtf8(wireType, out var decoded1)", decoder,
            StringComparison.Ordinal);
        Assert.DoesNotContain("TryReadString", decoder,
            StringComparison.Ordinal);
        Assert.Contains("global::System.Text.Encoding.UTF8.GetString(field1)", decoder,
            StringComparison.Ordinal);
        Assert.Contains("EncodingExtensions.GetString(global::System.Text.Encoding.UTF8, in field1)", decoder,
            StringComparison.Ordinal);
        Assert.Contains("global::System.ReadOnlySpan<byte> field2 = default;", decoder,
            StringComparison.Ordinal);
        Assert.Contains("global::System.Buffers.ReadOnlySequence<byte> field2 = default;", decoder,
            StringComparison.Ordinal);
        Assert.Contains("BuffersExtensions.ToArray(in field2)", decoder, StringComparison.Ordinal);

        int sequenceDecoderStart = decoder.IndexOf(
            "private readonly struct SequenceDecoder",
            StringComparison.Ordinal);
        string sequenceDecoder = decoder[sequenceDecoderStart..];
        int requiredCheck = sequenceDecoder.IndexOf("if (!seen3)", StringComparison.Ordinal);
        int stringMaterialization = sequenceDecoder.IndexOf(
            "EncodingExtensions.GetString",
            StringComparison.Ordinal);
        Assert.True(requiredCheck >= 0, sequenceDecoder);
        Assert.True(stringMaterialization > requiredCheck, sequenceDecoder);

        Assert.DoesNotContain("Tagged", decoder, StringComparison.Ordinal);
        Assert.DoesNotContain("BinaryFormatProfile", decoder, StringComparison.Ordinal);
        Assert.DoesNotContain("GeneratedDecoder", decoder, StringComparison.Ordinal);
        Assert.DoesNotContain("ITcpBinaryDecoder", decoder, StringComparison.Ordinal);
        Assert.DoesNotContain("TcpBinaryLimits", decoder, StringComparison.Ordinal);
        Assert.DoesNotContain("TcpBinaryDecodeError", decoder, StringComparison.Ordinal);
        Assert.DoesNotContain("TcpBinaryCoreAdapter", decoder, StringComparison.Ordinal);
        Assert.DoesNotContain("Encode", decoder, StringComparison.Ordinal);
        Assert.DoesNotContain("IBufferWriter", decoder, StringComparison.Ordinal);
        Assert.DoesNotContain("ITcpBinaryCodec", decoder, StringComparison.Ordinal);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(output.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void PublicDescriptorWithInternalContractUsesInternalDecodeEntries()
    {
        const string source = """
            using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

            internal sealed class Contract
            {
                public int Id { get; set; }
            }

            [TcpBinaryContract(typeof(Contract))]
            [TcpBinaryField(1, nameof(Contract.Id))]
            public static partial class Descriptor;
            """;

        GeneratorDriverRunResult result = RunGenerator(source, out Compilation output);
        GeneratorRunResult generator = Assert.Single(result.Results);
        string decoder = Assert.Single(generator.GeneratedSources).SourceText.ToString();

        Assert.Contains("public static partial class @Descriptor", decoder, StringComparison.Ordinal);
        Assert.Equal(
            2,
            CountOccurrences(
                decoder,
                "internal static global::ChatApp.Binary.Core.BinaryStatus TryDecode("));
        Assert.DoesNotContain(
            "public static global::ChatApp.Binary.Core.BinaryStatus TryDecode(",
            decoder,
            StringComparison.Ordinal);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(output.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData(new byte[] { 0x08, 0x02, 0x10, 0x04 }, BinaryStatus.Done)]
    [InlineData(new byte[] { 0x08, 0x02, 0x08, 0x04 }, BinaryStatus.DuplicateField)]
    [InlineData(new byte[] { 0x10, 0x04, 0x08, 0x02 }, BinaryStatus.FieldsOutOfOrder)]
    public void GeneratedSequenceDecoderPreservesCoreFieldOrderingStatus(
        byte[] payload,
        BinaryStatus expected)
    {
        const string source = """
            using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

            public sealed class Contract
            {
                public int First { get; set; }
                public int Second { get; set; }
            }

            [TcpBinaryContract(typeof(Contract))]
            [TcpBinaryField(1, nameof(Contract.First))]
            [TcpBinaryField(2, nameof(Contract.Second))]
            public static partial class Descriptor;
            """;

        _ = RunGenerator(source, out Compilation output);
        using var image = new MemoryStream();
        var emit = output.Emit(image);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(image.ToArray());
        Type descriptor = Assert.IsAssignableFrom<Type>(assembly.GetType("Descriptor"));
        MethodInfo method = Assert.Single(
            descriptor.GetMethods(BindingFlags.Public | BindingFlags.Static),
            candidate => candidate.Name == "TryDecode"
                         && candidate.GetParameters()[0].ParameterType.IsByRef);
        AssertSequenceDecode(method, new ReadOnlySequence<byte>(payload), expected);
        AssertSequenceDecode(method, CreateSegmented(payload), expected);
    }

    [Theory]
    [InlineData(
        new byte[] { 0x0A, 0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t' },
        BinaryStatus.MissingRequiredField)]
    [InlineData(
        new byte[] { 0x0A, 0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x12, 0x00 },
        BinaryStatus.WireTypeMismatch)]
    public void FailedSegmentedDecodeDoesNotMaterializePreviouslyValidatedString(
        byte[] payload,
        BinaryStatus expected)
    {
        const string source = """
            using System.Buffers;
            using ChatApp.Binary.Core;
            using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

            public sealed class Contract
            {
                public string Name { get; set; } = string.Empty;
                public int Count { get; set; }
                public required int RequiredId { get; set; }
            }

            [TcpBinaryContract(typeof(Contract))]
            [TcpBinaryField(1, nameof(Contract.Name))]
            [TcpBinaryField(2, nameof(Contract.Count))]
            [TcpBinaryField(3, nameof(Contract.RequiredId))]
            public static partial class Descriptor;

            public static class Probe
            {
                public static BinaryStatus Decode(
                    in ReadOnlySequence<byte> payload,
                    out bool hasValue)
                {
                    BinaryStatus status = Descriptor.TryDecode(
                        in payload,
                        BinaryLimits.Default,
                        out Contract? value);
                    hasValue = value is not null;
                    return status;
                }
            }
            """;

        _ = RunGenerator(source, out Compilation output);
        using var image = new MemoryStream();
        var emit = output.Emit(image);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(image.ToArray());
        MethodInfo method = Assert.IsAssignableFrom<Type>(assembly.GetType("Probe"))
            .GetMethod("Decode", BindingFlags.Public | BindingFlags.Static)!;
        var decode = method.CreateDelegate<StatusDecoder>();
        ReadOnlySequence<byte> sequence = CreateSegmented(payload);

        Assert.Equal(expected, decode(in sequence, out bool hasValue));
        Assert.False(hasValue);
        for (var index = 0; index < 16; index++)
        {
            _ = decode(in sequence, out _);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        BinaryStatus lastStatus = default;
        bool anyValue = false;
        for (var index = 0; index < 1_024; index++)
        {
            lastStatus = decode(in sequence, out hasValue);
            anyValue |= hasValue;
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(expected, lastStatus);
        Assert.False(anyValue);
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void SchemaSharedWithHandwrittenEncoderStillRequiresAPropertyGetter()
    {
        const string source = """
            using ChatApp.Shared.Protocol.Tcp.Binary.Generation;
            internal sealed class Contract
            {
                public int Id { set { } }
            }

            [TcpBinaryContract(typeof(Contract))]
            [TcpBinaryField(1, nameof(Contract.Id))]
            internal static partial class Descriptor;
            """;

        GeneratorDriverRunResult result = RunGenerator(source, out Compilation output);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CTB004");
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "CS8785");
        Assert.DoesNotContain(output.GetDiagnostics(), diagnostic => diagnostic.Id == "CS8785");
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
            [new TcpBinaryDecoderGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(input, out output, out _);
        return driver.GetRunResult();
    }

    private static MetadataReference[] GetMetadataReferences()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        IEnumerable<string> paths = trustedAssemblies.Split(Path.PathSeparator)
            .Append(typeof(TcpBinaryContractAttribute).Assembly.Location)
            .Append(typeof(global::ChatApp.Binary.Core.BinaryCodec).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return paths.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }

    private static ReadOnlySequence<byte> CreateSegmented(byte[] payload)
    {
        int split = Math.Max(1, payload.Length / 2);
        var first = new SequenceSegment(payload.AsMemory(0, split));
        SequenceSegment last = first.Append(payload.AsMemory(split));
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    private static void AssertSequenceDecode(
        MethodInfo method,
        ReadOnlySequence<byte> sequence,
        BinaryStatus expected)
    {
        object?[] arguments = [sequence, BinaryLimits.Default, null];

        var actual = Assert.IsType<BinaryStatus>(method.Invoke(null, arguments));

        Assert.Equal(expected, actual);
        Assert.Equal(expected == BinaryStatus.Done, arguments[2] is not null);
    }

    private sealed class SequenceSegment : ReadOnlySequenceSegment<byte>
    {
        public SequenceSegment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public SequenceSegment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new SequenceSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = segment;
            return segment;
        }
    }

    private delegate BinaryStatus StatusDecoder(
        in ReadOnlySequence<byte> payload,
        out bool hasValue);
}
