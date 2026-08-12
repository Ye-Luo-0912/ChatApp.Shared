namespace ChatApp.Shared.Protocol.Tcp.Binary;

/// <summary>
/// Identity of the first ChatApp binary payload protocol. The identifier is intentionally distinct
/// from Protobuf and from the discarded experimental tagged runtime.
/// </summary>
public static class BinaryPayloadFormat
{
    public const byte Version = 1;

    public const string Id = "chatapp-bin-v1";
}
