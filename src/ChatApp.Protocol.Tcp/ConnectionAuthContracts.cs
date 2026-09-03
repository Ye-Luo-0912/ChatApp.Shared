namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Client-to-server authentication request carried by <see cref="PacketCommand.AuthenticationRequest"/>.
/// The gateway currently serializes JSON; the canonical fields below are the target for the
/// negotiated binary-v1 subset.
/// </summary>
public sealed class AuthenticationRequest
{
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Device identity hash. Absent when the peer did not present one.</summary>
    public ulong? DeviceIdHash { get; set; }
}

/// <summary>
/// Server-to-client authentication result carried by <see cref="PacketCommand.AuthenticationResponse"/>.
/// </summary>
public sealed class AuthenticationResponse
{
    public bool Success { get; set; }

    public long UserId { get; set; }

    public string? ErrorMessage { get; set; }

    public string? SessionId { get; set; }

    /// <summary>Device identity hash echoed when the request carried one.</summary>
    public ulong? DeviceIdHash { get; set; }

    /// <summary>Server-issued authoritative device identifier echoed back for client confirmation.</summary>
    public string? DeviceId { get; set; }

    /// <summary>Resume token issued on success disclosure, usable for resumption.</summary>
    public string? ResumeToken { get; set; }
}

/// <summary>
/// Payload-less keep-alive marker carried by <see cref="PacketCommand.Heartbeat"/>. The frame
/// body is always empty; the instance exists so the binary registry returns a typed value.
/// </summary>
public sealed class Heartbeat
{
}

/// <summary>
/// Payload-less keep-alive acknowledgement marker carried by
/// <see cref="PacketCommand.HeartbeatAcknowledgement"/>. The frame body is always empty.
/// </summary>
public sealed class HeartbeatAcknowledgement
{
}