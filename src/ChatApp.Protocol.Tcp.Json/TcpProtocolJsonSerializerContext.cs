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
[JsonSerializable(typeof(MessageHistoryRequest))]
[JsonSerializable(typeof(MessageHistoryResponse))]
[JsonSerializable(typeof(MessageHistoryItem))]
[JsonSerializable(typeof(MessageHistoryItem[]))]
[JsonSerializable(typeof(MessageHistoryCursor))]
[JsonSerializable(typeof(TcpAttachmentRef))]
[JsonSerializable(typeof(TcpAttachmentRef[]))]
[JsonSerializable(typeof(MessageReactionSummary))]
[JsonSerializable(typeof(MessageReactionSummary[]))]
[JsonSerializable(typeof(SyncBootstrapRequest))]
[JsonSerializable(typeof(SyncBootstrapResponse))]
[JsonSerializable(typeof(ConversationSyncWatermark))]
[JsonSerializable(typeof(ConversationSyncWatermark[]))]
[JsonSerializable(typeof(ConversationHistoryCatchUp))]
[JsonSerializable(typeof(ConversationHistoryCatchUp[]))]
[JsonSerializable(typeof(TcpConversationListItem))]
[JsonSerializable(typeof(TcpConversationListItem[]))]
[JsonSerializable(typeof(TcpConversationListCursor))]
[JsonSerializable(typeof(SyncCursorResetRequired))]
[JsonSerializable(typeof(SyncCursorResetRequired[]))]
[JsonSerializable(typeof(RelationshipSyncWatermark))]
[JsonSerializable(typeof(RelationshipSyncWatermark[]))]
[JsonSerializable(typeof(RelationshipCatchUp))]
[JsonSerializable(typeof(RelationshipCatchUp[]))]
[JsonSerializable(typeof(RelationshipChangeLogEntry))]
[JsonSerializable(typeof(RelationshipChangeLogEntry[]))]
public partial class TcpProtocolJsonSerializerContext : JsonSerializerContext;
