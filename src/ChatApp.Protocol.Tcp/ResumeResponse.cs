namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Server-to-client session-resume result carried by <see cref="PacketCommand.ResumeResponse"/>.
/// </summary>
public sealed class ResumeResponse
{
    public bool Success { get; set; }

    public ResumeFailureKind FailureKind { get; set; }

    public string? ResumeToken { get; set; }

    public long UserId { get; set; }

    public string? SessionId { get; set; }

    public string? DeviceId { get; set; }

    public long? LastConversationSequence { get; set; }

    public string? ErrorMessage { get; set; }

    public int? RetryAfterMs { get; set; }
}

/// <summary>
/// Stable classification for unsuccessful resume attempts.
/// </summary>
public enum ResumeFailureKind : byte
{
    None = 0,
    InvalidToken = 1,
    DependencyUnavailable = 2,
    UserFrozen = 3
}
