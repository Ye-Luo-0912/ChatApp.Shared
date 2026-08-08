namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Server-to-client drain notification carried by <see cref="PacketCommand.GoAway"/>.
/// </summary>
public sealed class GoAway
{
    public int RetryAfterMs { get; set; }

    public string? Reason { get; set; }

    public string? ServerHint { get; set; }
}
