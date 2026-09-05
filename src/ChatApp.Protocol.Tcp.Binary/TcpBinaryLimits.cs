namespace ChatApp.Shared.Protocol.Tcp.Binary;

/// <summary>Immutable defensive limits applied before allocating or advancing a reader.</summary>
public readonly record struct TcpBinaryLimits
{
    public const int MaximumFieldNumber = 0x1FFF_FFFF;

    public static TcpBinaryLimits Default { get; } = new(
        maxMessageBytes: 80 * 1024,
        maxFieldBytes: 64 * 1024,
        maxStringBytes: 64 * 1024,
        maxByteArrayBytes: 64 * 1024,
        maxFields: 256,
        maxDepth: 16);

    public TcpBinaryLimits(
        int maxMessageBytes,
        int maxFieldBytes,
        int maxStringBytes,
        int maxByteArrayBytes,
        int maxFields,
        int maxDepth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMessageBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFieldBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStringBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxByteArrayBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFields);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDepth);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxFieldBytes, maxMessageBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxStringBytes, maxFieldBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxByteArrayBytes, maxFieldBytes);

        MaxMessageBytes = maxMessageBytes;
        MaxFieldBytes = maxFieldBytes;
        MaxStringBytes = maxStringBytes;
        MaxByteArrayBytes = maxByteArrayBytes;
        MaxFields = maxFields;
        MaxDepth = maxDepth;
    }

    public int MaxMessageBytes { get; }
    public int MaxFieldBytes { get; }
    public int MaxStringBytes { get; }
    public int MaxByteArrayBytes { get; }
    public int MaxFields { get; }

    /// <summary>Reserved for nested codecs; generated v1 scalar codecs remain at depth one.</summary>
    public int MaxDepth { get; }
}
