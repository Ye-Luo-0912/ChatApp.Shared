using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ChatApp.Binary.Core;
using ChatApp.Binary.Probe;

const int WarmupIterations = 20_000;
const int Iterations = 200_000;
const int Rounds = 7;

Console.WriteLine($"ChatApp binary-v1 Core probe | {Environment.Version} | {Environment.OSVersion} | {RuntimeInformation.ProcessArchitecture}");
ProbeState small = new(32);
ProbeState large = new(1024);
Workload[] workloads =
[
    new("single-pass encode", small, Encode, seed: 101),
    new("owning span decode", small, DecodeContiguous, seed: 103),
    new("owning sequence decode", small, DecodeSegmented, seed: 107),
    new("single-pass encode", large, Encode, seed: 109),
    new("owning span decode", large, DecodeContiguous, seed: 113),
    new("owning sequence decode", large, DecodeSegmented, seed: 127)
];

// Warm every generic/path/body combination before any timing. A short quiet period lets tiered
// compilation publish optimized code; the second alternating pass exercises that code uniformly.
WarmAll(workloads, WarmupIterations);
Thread.Sleep(250);
WarmAll(workloads, WarmupIterations);
Thread.Sleep(250);

for (var round = 0; round < Rounds; round++)
{
    // Rotate the first workload each round so no path consistently follows the two forced GCs.
    for (var offset = 0; offset < workloads.Length; offset++)
    {
        Workload workload = workloads[(round + offset) % workloads.Length];
        Measure(workload, round, Iterations);
    }
}

Console.WriteLine("Path                      Body Payload     ns/op     B/op             checksum");
foreach (Workload workload in workloads)
{
    Array.Sort(workload.Nanoseconds);
    Array.Sort(workload.BytesPerOperation);
    Console.WriteLine(
        $"{workload.Name,-25} {workload.State.BodySize,5} {workload.State.Payload.Length,7} " +
        $"{workload.Nanoseconds[Rounds / 2],9:F1} {workload.BytesPerOperation[Rounds / 2],8:F1} " +
        $"{workload.Checksum,20}");
}

RealTcpBinaryProbe.Run();

static void WarmAll(Workload[] workloads, int iterations)
{
    for (var iteration = 0; iteration < iterations; iteration++)
    {
        int start = iteration % workloads.Length;
        for (var offset = 0; offset < workloads.Length; offset++)
        {
            Workload workload = workloads[(start + offset) % workloads.Length];
            workload.Checksum = Mix(workload.Checksum, workload.Operation(workload.State));
        }
    }
}

static void Measure(Workload workload, int round, int iterations)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    long before = GC.GetAllocatedBytesForCurrentThread();
    long started = Stopwatch.GetTimestamp();
    long checksum = workload.Checksum;
    for (var iteration = 0; iteration < iterations; iteration++)
    {
        checksum = Mix(checksum, workload.Operation(workload.State));
    }

    long elapsed = Stopwatch.GetTimestamp() - started;
    long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    workload.Checksum = checksum;
    workload.Nanoseconds[round] =
        elapsed * 1_000_000_000D / Stopwatch.Frequency / iterations;
    workload.BytesPerOperation[round] = allocated / (double)iterations;
}

static long Mix(long checksum, long value) => unchecked((checksum * 31) + value);

static long Encode(ProbeState state)
{
    ProbeMessage message = state.Message;
    BinaryStatus status = BinaryCodec.TryEncode<ProbeEncoder, ProbeMessage>(
        in message,
        state.Buffer,
        BinaryLimits.Default,
        out int written);
    return status == BinaryStatus.Done ? written : -(long)status;
}

static long DecodeContiguous(ProbeState state)
{
    BinaryStatus status = BinaryCodec.TryDecode<ProbeDecoder, ProbeMessage>(
        state.Payload.Span,
        BinaryLimits.Default,
        out ProbeMessage? value);
    return status == BinaryStatus.Done ? value!.Id ^ value.Body.Length : -(long)status;
}

static long DecodeSegmented(ProbeState state)
{
    ReadOnlySequence<byte> sequence = state.Sequence;
    BinaryStatus status = BinaryCodec.TryDecode<ProbeSequenceDecoder, ProbeMessage>(
        in sequence,
        BinaryLimits.Default,
        out ProbeMessage? value);
    return status == BinaryStatus.Done ? value!.Id ^ value.Body.Length : -(long)status;
}

internal sealed record ProbeMessage(long Id, uint Kind, byte[] Body);

internal sealed class Workload
{
    public Workload(
        string name,
        ProbeState state,
        Func<ProbeState, long> operation,
        long seed)
    {
        Name = name;
        State = state;
        Operation = operation;
        Checksum = seed;
        Nanoseconds = new double[7];
        BytesPerOperation = new double[7];
    }

    public string Name { get; }

    public ProbeState State { get; }

    public Func<ProbeState, long> Operation { get; }

    public long Checksum { get; set; }

    public double[] Nanoseconds { get; }

    public double[] BytesPerOperation { get; }
}

internal readonly struct ProbeEncoder : IBinaryEncoder<ProbeEncoder, ProbeMessage>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ProbeMessage value)
    {
        writer.WriteInt64(1, value.Id);
        writer.WriteUInt32(2, value.Kind);
        return writer.WriteBytes(3, value.Body);
    }
}

internal readonly struct ProbeDecoder : IBinaryDecoder<ProbeDecoder, ProbeMessage>
{
    public static BinaryStatus Read(ref BinaryReadCursor reader, out ProbeMessage? value)
    {
        long id = 0;
        uint kind = 0;
        byte[] body = [];
        while (reader.TryReadFieldHeader(out int field, out BinaryWireType wire))
        {
            bool success = field switch
            {
                1 => reader.TryReadInt64(wire, out id),
                2 => reader.TryReadUInt32(wire, out kind),
                3 => ReadBody(ref reader, wire, out body),
                _ => reader.TrySkip(wire)
            };
            if (!success)
            {
                value = null;
                return reader.Status;
            }
        }

        value = reader.Status == BinaryStatus.Done ? new ProbeMessage(id, kind, body) : null;
        return reader.Status;
    }

    private static bool ReadBody(
        ref BinaryReadCursor reader,
        BinaryWireType wire,
        out byte[] value)
    {
        value = [];
        if (!reader.TryReadBytes(wire, out ReadOnlySpan<byte> borrowed))
        {
            return false;
        }

        value = borrowed.ToArray();
        return true;
    }
}

internal readonly struct ProbeSequenceDecoder : IBinarySequenceDecoder<ProbeSequenceDecoder, ProbeMessage>
{
    public static BinaryStatus Read(ref BinarySequenceReadCursor reader, out ProbeMessage? value)
    {
        long id = 0;
        uint kind = 0;
        byte[] body = [];
        while (reader.TryReadFieldHeader(out int field, out BinaryWireType wire))
        {
            bool success = field switch
            {
                1 => reader.TryReadInt64(wire, out id),
                2 => reader.TryReadUInt32(wire, out kind),
                3 => ReadBody(ref reader, wire, out body),
                _ => reader.TrySkip(wire)
            };
            if (!success)
            {
                value = null;
                return reader.Status;
            }
        }

        value = reader.Status == BinaryStatus.Done ? new ProbeMessage(id, kind, body) : null;
        return reader.Status;
    }

    private static bool ReadBody(
        ref BinarySequenceReadCursor reader,
        BinaryWireType wire,
        out byte[] value)
    {
        value = [];
        if (!reader.TryReadBytes(wire, out ReadOnlySequence<byte> borrowed))
        {
            return false;
        }

        value = borrowed.ToArray();
        return true;
    }
}

internal sealed class ProbeState
{
    public ProbeState(int bodySize)
    {
        BodySize = bodySize;
        Message = new ProbeMessage(42, 7, Enumerable.Range(0, bodySize).Select(static value => (byte)value).ToArray());
        Buffer = new byte[bodySize + 64];
        ProbeMessage message = Message;
        BinaryStatus status = BinaryCodec.TryEncode<ProbeEncoder, ProbeMessage>(
            in message,
            Buffer,
            BinaryLimits.Default,
            out int written);
        if (status != BinaryStatus.Done)
        {
            throw new InvalidOperationException($"Probe setup encode failed: {status}.");
        }

        Payload = Buffer.AsMemory(0, written);
        byte[] first = Payload.Span[..(written / 2)].ToArray();
        byte[] second = Payload.Span[(written / 2)..].ToArray();
        var head = new Segment(first);
        var tail = head.Append(new Segment(second));
        Sequence = new ReadOnlySequence<byte>(head, 0, tail, tail.Memory.Length);
    }

    public ProbeMessage Message { get; }

    public int BodySize { get; }

    public byte[] Buffer { get; }

    public ReadOnlyMemory<byte> Payload { get; }

    public ReadOnlySequence<byte> Sequence { get; }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public Segment Append(Segment next)
        {
            next.RunningIndex = RunningIndex + Memory.Length;
            Next = next;
            return next;
        }
    }
}
