using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Json;

namespace ChatApp.Binary.Probe;

internal static class RealTcpBinaryProbe
{
    private const int WarmupIterations = 10_000;
    private const int Iterations = 100_000;
    private const int Rounds = 5;
    private const int CorpusWarmupIterations = 2_000;
    private const int CorpusIterations = 20_000;
    private const int CorpusRounds = 3;
    private static readonly BinaryLimits Limits = BinaryLimits.Default;
    private static readonly BinaryLimits CorpusLimits = new(
        maxMessageBytes: 8 * 1024,
        maxFieldBytes: 2 * 1024,
        maxStringBytes: 1024,
        maxByteArrayBytes: 2 * 1024,
        maxFields: 16);

    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("Real TCP consumer schema probe | binary-v1 offline evidence");
        RunClientHello();
        RunMessageHistoryRequest();
        RunCorpus();
    }

    private static void RunClientHello()
    {
        var state = new ClientHelloState();
        PrintPayload("ClientHello", state.BinaryPayload, state.JsonPayload);
        RunWorkloads(
            "ClientHello",
            new RealWorkload[]
            {
                new("binary encode", () => EncodeClientHello(state)),
                new("json encode", () => JsonEncodeClientHello(state)),
                new("binary span decode", () => DecodeClientHello(state)),
                new("binary segmented decode", () => DecodeSegmentedClientHello(state)),
                new("json decode", () => JsonDecodeClientHello(state))
            });
    }

    private static void RunMessageHistoryRequest()
    {
        var state = new MessageHistoryRequestState();
        PrintPayload("MessageHistoryRequest", state.BinaryPayload, state.JsonPayload);
        RunWorkloads(
            "MessageHistoryRequest",
            new RealWorkload[]
            {
                new("binary encode", () => EncodeMessageHistoryRequest(state)),
                new("json encode", () => JsonEncodeMessageHistoryRequest(state)),
                new("binary span decode", () => DecodeMessageHistoryRequest(state)),
                new("binary segmented decode", () => DecodeSegmentedMessageHistoryRequest(state)),
                new("json decode", () => JsonDecodeMessageHistoryRequest(state))
            });
    }

    private static void PrintPayload(string name, byte[] binaryPayload, byte[] jsonPayload)
    {
        Console.WriteLine(
            $"{name}: binary={binaryPayload.Length} bytes " +
            $"sha256={Convert.ToHexString(SHA256.HashData(binaryPayload))}");
        Console.WriteLine(
            $"{name}: json={jsonPayload.Length} bytes " +
            $"sha256={Convert.ToHexString(SHA256.HashData(jsonPayload))}");
        Console.WriteLine($"{name}: binary-hex={Convert.ToHexString(binaryPayload)}");
    }

    private static void RunWorkloads(string schemaName, RealWorkload[] workloads)
    {
        foreach (RealWorkload workload in workloads)
        {
            for (var iteration = 0; iteration < WarmupIterations; iteration++)
            {
                workload.Checksum = Mix(workload.Checksum, workload.Operation());
            }
        }

        Thread.Sleep(100);
        foreach (RealWorkload workload in workloads)
        {
            for (var iteration = 0; iteration < WarmupIterations; iteration++)
            {
                workload.Checksum = Mix(workload.Checksum, workload.Operation());
            }
        }

        Thread.Sleep(100);
        for (var round = 0; round < Rounds; round++)
        {
            for (var offset = 0; offset < workloads.Length; offset++)
            {
                RealWorkload workload = workloads[(round + offset) % workloads.Length];
                Measure(workload, round);
            }
        }

        Console.WriteLine($"{schemaName} benchmark:");
        Console.WriteLine("Path                         ns/op     B/op             checksum");
        foreach (RealWorkload workload in workloads)
        {
            Array.Sort(workload.Nanoseconds);
            Array.Sort(workload.BytesPerOperation);
            Console.WriteLine(
                $"{workload.Name,-28} {workload.Nanoseconds[Rounds / 2],9:F1} " +
                $"{workload.BytesPerOperation[Rounds / 2],8:F1} {workload.Checksum,20}");
        }
    }

    private static void Measure(RealWorkload workload, int round)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        long started = Stopwatch.GetTimestamp();
        long checksum = workload.Checksum;
        for (var iteration = 0; iteration < Iterations; iteration++)
        {
            checksum = Mix(checksum, workload.Operation());
        }

        long elapsed = Stopwatch.GetTimestamp() - started;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        workload.Checksum = checksum;
        workload.Nanoseconds[round] =
            elapsed * 1_000_000_000D / Stopwatch.Frequency / Iterations;
        workload.BytesPerOperation[round] = allocated / (double)Iterations;
    }

    private static long EncodeClientHello(ClientHelloState state)
    {
        ClientHello value = state.Value;
        BinaryStatus status = ClientHelloProbeDescriptor.TryEncode(
            in value,
            state.BinaryBuffer,
            state.Limits,
            out int written);
        return status == BinaryStatus.Done ? written : -(long)status;
    }

    private static long JsonEncodeClientHello(ClientHelloState state) =>
        JsonSerializer.SerializeToUtf8Bytes(
            state.Value,
            TcpProtocolJsonSerializerContext.Default.ClientHello).Length;

    private static long DecodeClientHello(ClientHelloState state)
    {
        BinaryStatus status = ClientHelloProbeDescriptor.TryDecode(
            state.BinaryPayload.AsSpan(),
            state.Limits,
            out ClientHello? value);
        return status == BinaryStatus.Done ? Checksum(value!) : -(long)status;
    }

    private static long DecodeSegmentedClientHello(ClientHelloState state)
    {
        ReadOnlySequence<byte> sequence = state.BinarySequence;
        BinaryStatus status = ClientHelloProbeDescriptor.TryDecode(
            in sequence,
            state.Limits,
            out ClientHello? value);
        return status == BinaryStatus.Done ? Checksum(value!) : -(long)status;
    }

    private static long JsonDecodeClientHello(ClientHelloState state)
    {
        ClientHello? value = JsonSerializer.Deserialize(
            state.JsonPayload,
            TcpProtocolJsonSerializerContext.Default.ClientHello);
        return value is null ? -1 : Checksum(value);
    }

    private static long EncodeMessageHistoryRequest(MessageHistoryRequestState state)
    {
        MessageHistoryRequest value = state.Value;
        BinaryStatus status = MessageHistoryRequestProbeDescriptor.TryEncode(
            in value,
            state.BinaryBuffer,
            state.Limits,
            out int written);
        return status == BinaryStatus.Done ? written : -(long)status;
    }

    private static long JsonEncodeMessageHistoryRequest(MessageHistoryRequestState state) =>
        JsonSerializer.SerializeToUtf8Bytes(
            state.Value,
            TcpProtocolJsonSerializerContext.Default.MessageHistoryRequest).Length;

    private static long DecodeMessageHistoryRequest(MessageHistoryRequestState state)
    {
        BinaryStatus status = MessageHistoryRequestProbeDescriptor.TryDecode(
            state.BinaryPayload.AsSpan(),
            state.Limits,
            out MessageHistoryRequest? value);
        return status == BinaryStatus.Done ? Checksum(value!) : -(long)status;
    }

    private static long DecodeSegmentedMessageHistoryRequest(MessageHistoryRequestState state)
    {
        ReadOnlySequence<byte> sequence = state.BinarySequence;
        BinaryStatus status = MessageHistoryRequestProbeDescriptor.TryDecode(
            in sequence,
            state.Limits,
            out MessageHistoryRequest? value);
        return status == BinaryStatus.Done ? Checksum(value!) : -(long)status;
    }

    private static long JsonDecodeMessageHistoryRequest(MessageHistoryRequestState state)
    {
        MessageHistoryRequest? value = JsonSerializer.Deserialize(
            state.JsonPayload,
            TcpProtocolJsonSerializerContext.Default.MessageHistoryRequest);
        return value is null ? -1 : Checksum(value);
    }

    private static long Checksum(ClientHello value) =>
        unchecked((long)value.ProtocolVersion + value.FeatureBits + value.ClientTimeMs +
            (value.InstallationId?.Length ?? 0) + (value.ResumeToken?.Length ?? 0) +
            (value.MaxPayloadBytes ?? 0));

    private static long Checksum(MessageHistoryRequest value) =>
        unchecked((value.RequestId?.Length ?? 0) + (value.ConversationId?.Length ?? 0) +
            (value.BeforeReceivedAtMs ?? 0) + (value.BeforeMessageId?.Length ?? 0) +
            (value.AfterReceivedAtMs ?? 0) + (value.AfterMessageId?.Length ?? 0) + value.Limit);

    private static long Mix(long checksum, long value) => unchecked((checksum * 31) + value);

    private static void RunCorpus()
    {
        Console.WriteLine();
        Console.WriteLine("Real TCP schema corpus | 3-round median, 20,000 iterations");
        RunClientCorpusCase("ClientHello/default", new ClientHello(), Limits);
        RunClientCorpusCase(
            "ClientHello/string-heavy",
            new ClientHello
            {
                ProtocolVersion = 1,
                FeatureBits = uint.MaxValue,
                InstallationId = new string('界', 256),
                ClientTimeMs = 1_700_000_000_123,
                ResumeToken = new string('x', 512),
                MaxPayloadBytes = 80 * 1024
            },
            CorpusLimits);
        RunClientCorpusCase(
            "ClientHello/max-legal",
            new ClientHello
            {
                ProtocolVersion = 1,
                FeatureBits = uint.MaxValue,
                InstallationId = new string('界', 341),
                ClientTimeMs = long.MaxValue,
                ResumeToken = new string('x', 1024),
                MaxPayloadBytes = int.MaxValue
            },
            CorpusLimits);

        RunHistoryCorpusCase("MessageHistoryRequest/default", new MessageHistoryRequest(), Limits);
        RunHistoryCorpusCase(
            "MessageHistoryRequest/string-heavy",
            new MessageHistoryRequest
            {
                RequestId = new string('界', 256),
                ConversationId = new string('x', 512),
                BeforeReceivedAtMs = 1_700_000_000_100,
                BeforeMessageId = new string('b', 256),
                AfterReceivedAtMs = 1_700_000_000_200,
                AfterMessageId = new string('a', 512),
                Limit = 200
            },
            CorpusLimits);
        RunHistoryCorpusCase(
            "MessageHistoryRequest/max-legal",
            new MessageHistoryRequest
            {
                RequestId = new string('界', 341),
                ConversationId = new string('x', 1024),
                BeforeReceivedAtMs = long.MinValue,
                BeforeMessageId = new string('b', 341),
                AfterReceivedAtMs = long.MaxValue,
                AfterMessageId = new string('a', 1024),
                Limit = int.MaxValue
            },
            CorpusLimits);

        MeasureMalformedCorpus();
        Console.WriteLine("bytes-heavy coverage: Core probe includes 1,024-byte bytes payload; real TCP DTOs selected here have no canonical bytes field.");
    }

    private static void RunClientCorpusCase(string name, ClientHello value, BinaryLimits limits)
    {
        var state = new ClientHelloState(value, limits);
        PrintCorpusPayload(name, state.BinaryPayload, state.JsonPayload);
        MeasureCorpus(
            name,
            new RealWorkload[]
            {
                new("binary encode", () => EncodeClientHello(state)),
                new("binary span decode", () => DecodeClientHello(state)),
                new("binary segmented decode", () => DecodeSegmentedClientHello(state)),
                new("json encode", () => JsonEncodeClientHello(state)),
                new("json decode", () => JsonDecodeClientHello(state))
            });
    }

    private static void RunHistoryCorpusCase(
        string name,
        MessageHistoryRequest value,
        BinaryLimits limits)
    {
        var state = new MessageHistoryRequestState(value, limits);
        PrintCorpusPayload(name, state.BinaryPayload, state.JsonPayload);
        MeasureCorpus(
            name,
            new RealWorkload[]
            {
                new("binary encode", () => EncodeMessageHistoryRequest(state)),
                new("binary span decode", () => DecodeMessageHistoryRequest(state)),
                new("binary segmented decode", () => DecodeSegmentedMessageHistoryRequest(state)),
                new("json encode", () => JsonEncodeMessageHistoryRequest(state)),
                new("json decode", () => JsonDecodeMessageHistoryRequest(state))
            });
    }

    private static void PrintCorpusPayload(string name, byte[] binaryPayload, byte[] jsonPayload)
    {
        Console.WriteLine(
            $"{name}: binary={binaryPayload.Length}B json={jsonPayload.Length}B " +
            $"binary-sha256={Convert.ToHexString(SHA256.HashData(binaryPayload))}");
    }

    private static void MeasureCorpus(string name, RealWorkload[] workloads)
    {
        foreach (RealWorkload workload in workloads)
        {
            for (var iteration = 0; iteration < CorpusWarmupIterations; iteration++)
            {
                workload.Checksum = Mix(workload.Checksum, workload.Operation());
            }
        }

        Thread.Sleep(25);
        for (var round = 0; round < CorpusRounds; round++)
        {
            foreach (RealWorkload workload in workloads)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                long before = GC.GetAllocatedBytesForCurrentThread();
                long started = Stopwatch.GetTimestamp();
                long checksum = workload.Checksum;
                for (var iteration = 0; iteration < CorpusIterations; iteration++)
                {
                    checksum = Mix(checksum, workload.Operation());
                }

                long elapsed = Stopwatch.GetTimestamp() - started;
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                workload.Checksum = checksum;
                workload.Nanoseconds[round] =
                    elapsed * 1_000_000_000D / Stopwatch.Frequency / CorpusIterations;
                workload.BytesPerOperation[round] = allocated / (double)CorpusIterations;
            }
        }

        Console.WriteLine($"{name}:");
        foreach (RealWorkload workload in workloads)
        {
            Array.Sort(workload.Nanoseconds, 0, CorpusRounds);
            Array.Sort(workload.BytesPerOperation, 0, CorpusRounds);
            Console.WriteLine(
                $"  {workload.Name,-24} {workload.Nanoseconds[CorpusRounds / 2],8:F1} ns/op " +
                $"{workload.BytesPerOperation[CorpusRounds / 2],7:F1} B/op");
        }
    }

    private static void MeasureMalformedCorpus()
    {
        Console.WriteLine("Malformed corpus:");
        MeasureMalformed(
            "ClientHello/invalid-utf8",
            [0x1A, 0x01, 0xFF],
            static (payload, limits) =>
            {
                BinaryStatus status = ClientHelloProbeDescriptor.TryDecode(
                    payload,
                    limits,
                    out ClientHello? value);
                return value is null ? (long)status : -1;
            },
            BinaryStatus.InvalidUtf8);
        MeasureMalformed(
            "ClientHello/truncated",
            [0x1A, 0x02, 0x61],
            static (payload, limits) =>
            {
                BinaryStatus status = ClientHelloProbeDescriptor.TryDecode(
                    payload,
                    limits,
                    out ClientHello? value);
                return value is null ? (long)status : -1;
            },
            BinaryStatus.Truncated);
        MeasureMalformed(
            "MessageHistoryRequest/invalid-utf8",
            [0x0A, 0x01, 0xFF],
            static (payload, limits) =>
            {
                BinaryStatus status = MessageHistoryRequestProbeDescriptor.TryDecode(
                    payload,
                    limits,
                    out MessageHistoryRequest? value);
                return value is null ? (long)status : -1;
            },
            BinaryStatus.InvalidUtf8);
        MeasureMalformed(
            "MessageHistoryRequest/truncated",
            [0x0A, 0x02, 0x61],
            static (payload, limits) =>
            {
                BinaryStatus status = MessageHistoryRequestProbeDescriptor.TryDecode(
                    payload,
                    limits,
                    out MessageHistoryRequest? value);
                return value is null ? (long)status : -1;
            },
            BinaryStatus.Truncated);
    }

    private static void MeasureMalformed(
        string name,
        byte[] payload,
        Func<ReadOnlySpan<byte>, BinaryLimits, long> operation,
        BinaryStatus expected)
    {
        for (var iteration = 0; iteration < CorpusWarmupIterations; iteration++)
        {
            if (operation(payload, Limits) != (long)expected)
            {
                throw new InvalidOperationException($"Malformed corpus status mismatch: {name}.");
            }
        }

        long started = Stopwatch.GetTimestamp();
        long checksum = 0;
        for (var iteration = 0; iteration < CorpusIterations; iteration++)
        {
            checksum = Mix(checksum, operation(payload, Limits));
        }

        double ns = (Stopwatch.GetTimestamp() - started) * 1_000_000_000D /
            Stopwatch.Frequency / CorpusIterations;
        Console.WriteLine($"  {name,-36} {ns,8:F1} ns/op status={expected} checksum={checksum}");
    }

    private sealed class RealWorkload(string name, Func<long> operation)
    {
        public string Name { get; } = name;

        public Func<long> Operation { get; } = operation;

        public long Checksum { get; set; }

        public double[] Nanoseconds { get; } = new double[Rounds];

        public double[] BytesPerOperation { get; } = new double[Rounds];
    }

    private sealed class ClientHelloState
    {
        public ClientHelloState()
            : this(
                new ClientHello
                {
                    ProtocolVersion = 1,
                    FeatureBits = 0xA5,
                    InstallationId = "install-01",
                    ClientTimeMs = 1_700_000_000_123,
                    ResumeToken = "resume-token",
                    MaxPayloadBytes = 80 * 1024
                },
                RealTcpBinaryProbe.Limits)
        {
        }

        public ClientHelloState(ClientHello value, BinaryLimits limits)
        {
            Value = value;
            Limits = limits;
            BinaryBuffer = new byte[Math.Min(limits.MaxMessageBytes, 8 * 1024)];
            BinaryPayload = Encode(Value, BinaryBuffer, limits, ClientHelloProbeDescriptor.TryEncode);
            JsonPayload = JsonSerializer.SerializeToUtf8Bytes(
                Value,
                TcpProtocolJsonSerializerContext.Default.ClientHello);
            BinarySequence = Segment(BinaryPayload);
        }

        public ClientHello Value { get; }

        public BinaryLimits Limits { get; }

        public byte[] BinaryBuffer { get; }

        public byte[] BinaryPayload { get; }

        public byte[] JsonPayload { get; }

        public ReadOnlySequence<byte> BinarySequence { get; }
    }

    private sealed class MessageHistoryRequestState
    {
        public MessageHistoryRequestState()
            : this(
                new MessageHistoryRequest
                {
                    RequestId = "history-1",
                    ConversationId = "conversation-1",
                    BeforeReceivedAtMs = 1_700_000_000_100,
                    BeforeMessageId = "msg-before",
                    AfterReceivedAtMs = 1_700_000_000_200,
                    AfterMessageId = "msg-after",
                    Limit = 50
                },
                RealTcpBinaryProbe.Limits)
        {
        }

        public MessageHistoryRequestState(MessageHistoryRequest value, BinaryLimits limits)
        {
            Value = value;
            Limits = limits;
            BinaryBuffer = new byte[Math.Min(limits.MaxMessageBytes, 8 * 1024)];
            BinaryPayload = Encode(Value, BinaryBuffer, limits, MessageHistoryRequestProbeDescriptor.TryEncode);
            JsonPayload = JsonSerializer.SerializeToUtf8Bytes(
                Value,
                TcpProtocolJsonSerializerContext.Default.MessageHistoryRequest);
            BinarySequence = Segment(BinaryPayload);
        }

        public MessageHistoryRequest Value { get; }

        public BinaryLimits Limits { get; }

        public byte[] BinaryBuffer { get; }

        public byte[] BinaryPayload { get; }

        public byte[] JsonPayload { get; }

        public ReadOnlySequence<byte> BinarySequence { get; }
    }

    private delegate BinaryStatus Encoder<T>(
        in T value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written);

    private static byte[] Encode<T>(
        T value,
        byte[] destination,
        BinaryLimits limits,
        Encoder<T> encoder)
    {
        BinaryStatus status = encoder(in value, destination, limits, out int written);
        if (status != BinaryStatus.Done)
        {
            throw new InvalidOperationException($"Real schema setup encode failed: {status}.");
        }

        return destination[..written].ToArray();
    }

    private static ReadOnlySequence<byte> Segment(byte[] payload)
    {
        int firstLength = Math.Max(1, payload.Length / 3);
        int secondLength = Math.Max(firstLength + 1, (payload.Length * 2) / 3);
        secondLength = Math.Min(payload.Length - 1, secondLength);
        var head = new ProbeSegment(payload.AsMemory(0, firstLength));
        var middle = head.Append(new ProbeSegment(payload.AsMemory(firstLength, secondLength - firstLength)));
        var tail = middle.Append(new ProbeSegment(payload.AsMemory(secondLength)));
        return new ReadOnlySequence<byte>(head, 0, tail, tail.Memory.Length);
    }

    private sealed class ProbeSegment : ReadOnlySequenceSegment<byte>
    {
        public ProbeSegment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public ProbeSegment Append(ProbeSegment next)
        {
            next.RunningIndex = RunningIndex + Memory.Length;
            Next = next;
            return next;
        }
    }
}
