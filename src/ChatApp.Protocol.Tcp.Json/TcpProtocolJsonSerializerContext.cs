using System.Text.Json.Serialization;
using ChatApp.Shared.Protocol.Tcp;

namespace ChatApp.Shared.Protocol.Tcp.Json;

/// <summary>
/// Source-generated JSON metadata for TCP control-plane contracts.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = false,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ClientHello))]
[JsonSerializable(typeof(ServerHello))]
[JsonSerializable(typeof(GoAway))]
[JsonSerializable(typeof(ResumeResponse))]
[JsonSerializable(typeof(ProtocolErrorFrame))]
public partial class TcpProtocolJsonSerializerContext : JsonSerializerContext;
