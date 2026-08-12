namespace ChatApp.Binary.Core;

/// <summary>
/// Immutable budgets checked before consuming a bounded field body or allocating owning output.
/// A decoder may consume the already-bounded tag and length prefix before it can classify a limit.
/// </summary>
public readonly record struct BinaryLimits
{
    public const int MaximumFieldNumber = 0x1FFF_FFFF;

    public static BinaryLimits Default { get; } = new(
        maxMessageBytes: 80 * 1024,
        maxFieldBytes: 64 * 1024,
        maxStringBytes: 64 * 1024,
        maxByteArrayBytes: 64 * 1024,
        maxFields: 256);

    public BinaryLimits(
        int maxMessageBytes,
        int maxFieldBytes,
        int maxStringBytes,
        int maxByteArrayBytes,
        int maxFields)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMessageBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFieldBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStringBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxByteArrayBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFields);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxFieldBytes, maxMessageBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxStringBytes, maxFieldBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxByteArrayBytes, maxFieldBytes);

        MaxMessageBytes = maxMessageBytes;
        MaxFieldBytes = maxFieldBytes;
        MaxStringBytes = maxStringBytes;
        MaxByteArrayBytes = maxByteArrayBytes;
        MaxFields = maxFields;
    }

    public int MaxMessageBytes { get; }

    public int MaxFieldBytes { get; }

    public int MaxStringBytes { get; }

    public int MaxByteArrayBytes { get; }

    public int MaxFields { get; }

    internal void Validate(string parameterName)
    {
        if (MaxMessageBytes <= 0
            || MaxFieldBytes <= 0
            || MaxStringBytes <= 0
            || MaxByteArrayBytes <= 0
            || MaxFields <= 0
            || MaxFieldBytes > MaxMessageBytes
            || MaxStringBytes > MaxFieldBytes
            || MaxByteArrayBytes > MaxFieldBytes)
        {
            throw new ArgumentException(
                "Binary limits must be created with the validating constructor.",
                parameterName);
        }
    }
}
