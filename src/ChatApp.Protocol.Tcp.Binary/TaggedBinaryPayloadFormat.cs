namespace ChatApp.Shared.Protocol.Tcp.Binary;

/// <summary>
/// Stable identifier for the ChatApp bounded tagged payload format.
/// This identifier is intentionally not advertised by the gateway yet. Once negotiated, the
/// connection-level identifier selects the wire version, so payloads do not repeat a version byte.
/// </summary>
public static class TaggedBinaryPayloadFormat
{
    public const byte Version = 1;

    public const string Id = "chatapp-tagged-v1";
}
