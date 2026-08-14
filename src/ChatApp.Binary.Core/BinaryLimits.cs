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
        maxFields: 256,
        maxCollectionElements: 256,
        maxNestingDepth: 8,
        maxMaterializedBytes: 512 * 1024);

    public BinaryLimits(
        int maxMessageBytes,
        int maxFieldBytes,
        int maxStringBytes,
        int maxByteArrayBytes,
        int maxFields,
        int maxCollectionElements = 256,
        int maxNestingDepth = 8,
        int maxMaterializedBytes = 512 * 1024,
        int currentNestingDepth = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMessageBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFieldBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStringBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxByteArrayBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFields);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCollectionElements);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxNestingDepth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMaterializedBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(currentNestingDepth);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxFieldBytes, maxMessageBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxStringBytes, maxFieldBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxByteArrayBytes, maxFieldBytes);

        MaxMessageBytes = maxMessageBytes;
        MaxFieldBytes = maxFieldBytes;
        MaxStringBytes = maxStringBytes;
        MaxByteArrayBytes = maxByteArrayBytes;
        MaxFields = maxFields;
        MaxCollectionElements = maxCollectionElements;
        MaxNestingDepth = maxNestingDepth;
        MaxMaterializedBytes = maxMaterializedBytes;
        CurrentNestingDepth = currentNestingDepth;
    }

    public int MaxMessageBytes { get; }

    public int MaxFieldBytes { get; }

    public int MaxStringBytes { get; }

    public int MaxByteArrayBytes { get; }

    public int MaxFields { get; }

    public int MaxCollectionElements { get; }

    public int MaxNestingDepth { get; }

    public int MaxMaterializedBytes { get; }

    internal int CurrentNestingDepth { get; }

    /// <summary>Returns the child-message view of these limits without changing any configured budget.</summary>
    public BinaryLimits ForNestedMessage() => new(
        maxMessageBytes: MaxFieldBytes,
        maxFieldBytes: MaxFieldBytes,
        maxStringBytes: Math.Min(MaxStringBytes, MaxFieldBytes),
        maxByteArrayBytes: Math.Min(MaxByteArrayBytes, MaxFieldBytes),
        maxFields: MaxFields,
        maxCollectionElements: MaxCollectionElements,
        maxNestingDepth: MaxNestingDepth,
        maxMaterializedBytes: MaxMaterializedBytes,
        currentNestingDepth: checked(CurrentNestingDepth + 1));

    internal void Validate(string parameterName)
    {
        if (MaxMessageBytes <= 0
            || MaxFieldBytes <= 0
            || MaxStringBytes <= 0
            || MaxByteArrayBytes <= 0
            || MaxFields <= 0
            || MaxCollectionElements <= 0
            || MaxNestingDepth <= 0
            || MaxMaterializedBytes <= 0
            || CurrentNestingDepth < 0
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
