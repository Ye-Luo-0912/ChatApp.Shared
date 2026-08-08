namespace ChatApp.Contracts.Http.Sessions;

public sealed class SessionDevice
{
    public string DeviceId { get; init; } = string.Empty;
    public string? DeviceName { get; init; }
    public string? DeviceType { get; init; }
    public string? ClientIp { get; init; }
    public string? UserAgent { get; init; }
    public DateTime LoginAt { get; init; }
    public DateTime LastActiveAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string? SessionId { get; init; }
    public int RefreshCount { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed class RevokeSessionsResponse
{
    public int Revoked { get; init; }
}
