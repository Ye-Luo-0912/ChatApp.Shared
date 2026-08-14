namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Capability bits exchanged in <see cref="ClientHello.FeatureBits"/> and
/// <see cref="ServerHello.FeatureBits"/>.
/// </summary>
[Flags]
public enum GatewayFeature : uint
{
    None = 0,
    BinaryPayload = 1u << 0,
    Compression = 1u << 1,
    StreamingChat = 1u << 2,
    CommandCapabilities = 1u << 3,
    SessionResume = 1u << 4,
    ConversationSync = 1u << 5,
    ConversationPreferences = 1u << 6,
    MessageMutation = 1u << 7,
    PresenceAndTyping = 1u << 8,
    MessageReactions = 1u << 9,
    GroupManagement = 1u << 10,
    PushTokenManagement = 1u << 11,
    CallSignaling = 1u << 12,
    RelationshipRead = 1u << 13
}

/// <summary>
/// Stable payload-format identifiers used by <see cref="ServerHello.PayloadFormat"/>.
/// </summary>
public static class ProtocolPayloadFormat
{
    public const string Json = "json";
    public const string Protobuf = "pb";

    public static bool IsValid(string? format) => format is Json or Protobuf;
}
