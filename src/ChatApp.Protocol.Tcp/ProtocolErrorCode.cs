namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Stable protocol errors carried by <see cref="PacketCommand.Error"/> frames.
/// </summary>
public enum ProtocolErrorCode : ushort
{
    None = 0,
    ProtocolViolation = 1,
    UnsupportedCommand = 2,
    UnsupportedVersion = 3,
    InvalidPayload = 4,
    AuthRequired = 10,
    AuthRejected = 11,
    SessionRevoked = 12,
    ResumeFailed = 13,
    DependencyUnavailable = 14,
    AccountSuspended = 15,
    RateLimited = 20,
    PayloadTooLarge = 21,
    FeatureNotNegotiated = 22,
    ServerOverloaded = 30,
    Shutdown = 31,
    OutboundQueueFull = 32,
    InternalError = 99
}

public static class ProtocolErrorCodeExtensions
{
    public static bool IsFatal(this ProtocolErrorCode code) => code is
        ProtocolErrorCode.ProtocolViolation or
        ProtocolErrorCode.UnsupportedCommand or
        ProtocolErrorCode.UnsupportedVersion or
        ProtocolErrorCode.InvalidPayload;
}
