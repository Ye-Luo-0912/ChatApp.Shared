using System.Buffers;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary;

namespace ChatApp.Shared.Protocol.Tcp.Binary.Schemas;

/// <summary>
/// Stable outcome of a wire-codec dispatch. When <see cref="Status"/> is
/// <see cref="TcpBinaryWireStatus.Decoded"/>, <see cref="Value"/> holds the schema-authored
/// DTO for the covered command; otherwise the caller must fail closed and must not use
/// <see cref="Value"/>.
/// </summary>
public enum TcpBinaryWireStatus : byte
{
    /// <summary>The command is part of the negotiated subset and its payload decoded.</summary>
    Decoded = 0,

    /// <summary>The command has no binary-v1 schema; the connection must fail closed.</summary>
    SchemaNotCovered,

    /// <summary>A covered command's payload was malformed or over-limit; see <see cref="TcpBinaryWireDecode.DecodeStatus"/>.</summary>
    DecodeFailure,
}

/// <summary>Result of dispatching a frame to the command-to-schema registry.</summary>
public readonly struct TcpBinaryWireDecode
{
    public static TcpBinaryWireDecode NotCovered { get; } =
        new(TcpBinaryWireStatus.SchemaNotCovered, BinaryStatus.Done, null);

    public static TcpBinaryWireDecode Failure(BinaryStatus decodeStatus) =>
        new(TcpBinaryWireStatus.DecodeFailure, decodeStatus, null);

    public static TcpBinaryWireDecode Success(object value) =>
        new(TcpBinaryWireStatus.Decoded, BinaryStatus.Done, value);

    private TcpBinaryWireDecode(TcpBinaryWireStatus status, BinaryStatus decodeStatus, object? value)
    {
        Status = status;
        DecodeStatus = decodeStatus;
        Value = value;
    }

    public TcpBinaryWireStatus Status { get; }

    /// <summary>Populated only when <see cref="Status"/> is <see cref="TcpBinaryWireStatus.DecodeFailure"/>.</summary>
    public BinaryStatus DecodeStatus { get; }

    public object? Value { get; }
}

/// <summary>
/// The command-to-schema registry for the negotiated <c>chatapp-bin-v1</c> subset. It owns the
/// canonical mapping from <see cref="PacketCommand"/> to a schema descriptor and the fail-closed
/// contract: any command without a schema, any malformed or over-limit payload for a covered
/// command, and any format that is not the negotiated binary id returns a stable non-success
/// outcome. Handshake frames remain JSON and are never decoded over the binary path in production;
/// the registry still recognises them so the subset can be validated and goldened offline.
/// </summary>
public static class TcpBinaryWireCodec
{
    /// <summary>The exact negotiated format id that this registry recognises.</summary>
    public const string NegotiatedFormatId = BinaryPayloadFormat.Id;

    /// <summary>Shared, allocation-free marker decoded for the two payload-less keep-alive frames.</summary>
    private static readonly Heartbeat HeartbeatMarker = new();

    /// <summary>Shared, allocation-free marker decoded for the payload-less keep-alive acknowledgement.</summary>
    private static readonly HeartbeatAcknowledgement HeartbeatAcknowledgementMarker = new();

    /// <summary>Negotiation identification: true only for the exact binary-v1 format id.</summary>
    public static bool IsNegotiatedPayloadFormat(string? payloadFormat) =>
        string.Equals(payloadFormat, BinaryPayloadFormat.Id, StringComparison.Ordinal);

    /// <summary>
    /// Dispatches a contiguous frame payload to the schema for <paramref name="command"/>.
    /// Uncovered commands and malformed covered payloads fail closed.
    /// </summary>
    public static TcpBinaryWireDecode TryDecode(
        PacketCommand command,
        ReadOnlySpan<byte> payload,
        BinaryLimits limits)
    {
        return command switch
        {
            PacketCommand.ClientHello =>
                FromStatus(ClientHelloSchema.TryDecode(payload, limits, out ClientHello? clientHello), clientHello),
            PacketCommand.ServerHello =>
                FromStatus(ServerHelloSchema.TryDecode(payload, limits, out ServerHello? serverHello), serverHello),
            PacketCommand.GoAway =>
                FromStatus(GoAwaySchema.TryDecode(payload, limits, out GoAway? goAway), goAway),
            PacketCommand.ResumeResponse =>
                FromStatus(ResumeResponseSchema.TryDecode(payload, limits, out ResumeResponse? resumeResponse), resumeResponse),
            PacketCommand.Error =>
                FromStatus(ProtocolErrorFrameSchema.TryDecode(payload, limits, out ProtocolErrorFrame? frame), frame),
            PacketCommand.MessageHistoryRequest =>
                FromStatus(MessageHistoryRequestSchema.TryDecode(payload, limits, out MessageHistoryRequest? request), request),
            PacketCommand.MessageHistoryPage =>
                FromStatus(MessageHistoryResponseSchema.TryDecode(payload, limits, out MessageHistoryResponse? response), response),
            PacketCommand.AuthenticationRequest =>
                FromStatus(AuthenticationRequestSchema.TryDecode(payload, limits, out AuthenticationRequest? authRequest), authRequest),
            PacketCommand.AuthenticationResponse =>
                FromStatus(AuthenticationResponseSchema.TryDecode(payload, limits, out AuthenticationResponse? authResponse), authResponse),
            PacketCommand.Heartbeat =>
                DecodeEmptyFrame(payload, HeartbeatMarker),
            PacketCommand.HeartbeatAcknowledgement =>
                DecodeEmptyFrame(payload, HeartbeatAcknowledgementMarker),
            PacketCommand.ChatMessage =>
                FromStatus(ChatMessageSchema.TryDecode(payload, limits, out ChatMessage? chatMessage), chatMessage),
            PacketCommand.MessageAcknowledgement =>
                FromStatus(MessageAcknowledgementSchema.TryDecode(payload, limits, out MessageAcknowledgement? messageAcknowledgement), messageAcknowledgement),
            PacketCommand.MessageReceipt =>
                FromStatus(MessageReceiptSchema.TryDecode(payload, limits, out MessageReceipt? messageReceipt), messageReceipt),
            PacketCommand.MessageReceiptAcknowledgement =>
                FromStatus(MessageReceiptAcknowledgementSchema.TryDecode(payload, limits, out MessageReceiptAcknowledgement? receiptAcknowledgement), receiptAcknowledgement),
            PacketCommand.MessageReceiptUpdated =>
                FromStatus(MessageReceiptUpdatedSchema.TryDecode(payload, limits, out MessageReceiptUpdated? receiptUpdated), receiptUpdated),
            PacketCommand.AttachmentLifecycleChanged =>
                FromStatus(AttachmentLifecycleChangedSchema.TryDecode(payload, limits, out AttachmentLifecycleChanged? lifecycle), lifecycle),
            PacketCommand.AttachmentFinalizeRequest =>
                FromStatus(AttachmentFinalizeRequestSchema.TryDecode(payload, limits, out AttachmentFinalizeRequest? finalizeRequest), finalizeRequest),
            PacketCommand.AttachmentFinalizeResponse =>
                FromStatus(AttachmentFinalizeResponseSchema.TryDecode(payload, limits, out AttachmentFinalizeResponse? finalizeResponse), finalizeResponse),
            PacketCommand.AttachmentDownloadAuthorizeRequest =>
                FromStatus(AttachmentDownloadAuthorizeRequestSchema.TryDecode(payload, limits, out AttachmentDownloadAuthorizeRequest? downloadAuthorizeRequest), downloadAuthorizeRequest),
            PacketCommand.AttachmentDownloadAuthorizeResponse =>
                FromStatus(AttachmentDownloadAuthorizeResponseSchema.TryDecode(payload, limits, out AttachmentDownloadAuthorizeResponse? downloadAuthorizeResponse), downloadAuthorizeResponse),
            PacketCommand.CallCommandRequest =>
                FromStatus(TcpCallCommandRequestSchema.TryDecode(payload, limits, out TcpCallCommandRequest? callCommandRequest), callCommandRequest),
            PacketCommand.CallCommandResponse =>
                FromStatus(TcpCallCommandResponseSchema.TryDecode(payload, limits, out TcpCallCommandResponse? callCommandResponse), callCommandResponse),
            PacketCommand.CallSignal =>
                FromStatus(TcpCallSignalSchema.TryDecode(payload, limits, out TcpCallSignal? callSignal), callSignal),
            PacketCommand.MessageEditRequest =>
                FromStatus(MessageEditRequestSchema.TryDecode(payload, limits, out MessageEditRequest? editRequest), editRequest),
            PacketCommand.MessageEditAck =>
                FromStatus(MessageEditAcknowledgementSchema.TryDecode(payload, limits, out MessageEditAcknowledgement? editAck), editAck),
            PacketCommand.MessageEdited =>
                FromStatus(MessageEditedUpdateSchema.TryDecode(payload, limits, out MessageEditedUpdate? edited), edited),
            PacketCommand.MessageRecallRequest =>
                FromStatus(MessageRecallRequestSchema.TryDecode(payload, limits, out MessageRecallRequest? recallRequest), recallRequest),
            PacketCommand.MessageRecallAck =>
                FromStatus(MessageRecallAcknowledgementSchema.TryDecode(payload, limits, out MessageRecallAcknowledgement? recallAck), recallAck),
            PacketCommand.MessageRecalled =>
                FromStatus(MessageRecalledUpdateSchema.TryDecode(payload, limits, out MessageRecalledUpdate? recalled), recalled),
            PacketCommand.AddReactionRequest =>
                FromStatus(AddReactionRequestSchema.TryDecode(payload, limits, out AddReactionRequest? addReactionRequest), addReactionRequest),
            PacketCommand.AddReactionAck =>
                FromStatus(AddReactionAcknowledgementSchema.TryDecode(payload, limits, out AddReactionAcknowledgement? addReactionAck), addReactionAck),
            PacketCommand.ReactionAdded =>
                FromStatus(ReactionAddedUpdateSchema.TryDecode(payload, limits, out ReactionAddedUpdate? reactionAdded), reactionAdded),
            PacketCommand.RemoveReactionRequest =>
                FromStatus(RemoveReactionRequestSchema.TryDecode(payload, limits, out RemoveReactionRequest? removeReactionRequest), removeReactionRequest),
            PacketCommand.RemoveReactionAck =>
                FromStatus(RemoveReactionAcknowledgementSchema.TryDecode(payload, limits, out RemoveReactionAcknowledgement? removeReactionAck), removeReactionAck),
            PacketCommand.ReactionRemoved =>
                FromStatus(ReactionRemovedUpdateSchema.TryDecode(payload, limits, out ReactionRemovedUpdate? reactionRemoved), reactionRemoved),
            PacketCommand.ConversationListRequest =>
                FromStatus(ConversationListRequestSchema.TryDecode(payload, limits, out ConversationListRequest? conversationListRequest), conversationListRequest),
            PacketCommand.ConversationListPage =>
                FromStatus(ConversationListPageSchema.TryDecode(payload, limits, out ConversationListPage? conversationListPage), conversationListPage),
            PacketCommand.ConversationMarkReadRequest =>
                FromStatus(ConversationMarkReadRequestSchema.TryDecode(payload, limits, out ConversationMarkReadRequest? markReadRequest), markReadRequest),
            PacketCommand.ConversationMarkReadResponse =>
                FromStatus(ConversationMarkReadResponseSchema.TryDecode(payload, limits, out ConversationMarkReadResponse? markReadResponse), markReadResponse),
            PacketCommand.ConversationChanged =>
                FromStatus(ConversationChangedUpdateSchema.TryDecode(payload, limits, out ConversationChangedUpdate? conversationChanged), conversationChanged),
            PacketCommand.UnreadCountChanged =>
                FromStatus(UnreadCountChangedSchema.TryDecode(payload, limits, out UnreadCountChanged? unreadCountChanged), unreadCountChanged),
            PacketCommand.SyncBootstrapRequest =>
                FromStatus(SyncBootstrapRequestSchema.TryDecode(payload, limits, out SyncBootstrapRequest? syncBootstrapRequest), syncBootstrapRequest),
            PacketCommand.SyncBootstrapResponse =>
                FromStatus(SyncBootstrapResponseSchema.TryDecode(payload, limits, out SyncBootstrapResponse? syncBootstrapResponse), syncBootstrapResponse),
            PacketCommand.ConversationSetPrefsRequest =>
                FromStatus(ConversationSetPrefsRequestSchema.TryDecode(payload, limits, out ConversationSetPrefsRequest? setPrefsRequest), setPrefsRequest),
            PacketCommand.ConversationSetPrefsResponse =>
                FromStatus(ConversationSetPrefsResponseSchema.TryDecode(payload, limits, out ConversationSetPrefsResponse? setPrefsResponse), setPrefsResponse),
            PacketCommand.ConversationRead =>
                FromStatus(ConversationReadUpdateSchema.TryDecode(payload, limits, out ConversationReadUpdate? conversationRead), conversationRead),
            PacketCommand.MessageReadReceiptQueryRequest =>
                FromStatus(MessageReadReceiptQueryRequestSchema.TryDecode(payload, limits, out MessageReadReceiptQueryRequest? readReceiptQueryRequest), readReceiptQueryRequest),
            PacketCommand.MessageReadReceiptQueryResponse =>
                FromStatus(MessageReadReceiptQueryResponseSchema.TryDecode(payload, limits, out MessageReadReceiptQueryResponse? readReceiptQueryResponse), readReceiptQueryResponse),
            PacketCommand.RelationshipListChanged =>
                FromStatus(TcpRelationshipListChangedUpdateSchema.TryDecode(payload, limits, out TcpRelationshipListChangedUpdate? relationshipListChanged), relationshipListChanged),
            PacketCommand.RelationshipCommandRequest =>
                FromStatus(TcpRelationshipCommandRequestSchema.TryDecode(payload, limits, out TcpRelationshipCommandRequest? relationshipCommandRequest), relationshipCommandRequest),
            PacketCommand.RelationshipCommandResponse =>
                FromStatus(TcpRelationshipCommandResponseSchema.TryDecode(payload, limits, out TcpRelationshipCommandResponse? relationshipCommandResponse), relationshipCommandResponse),
            PacketCommand.RelationshipListRequest =>
                FromStatus(TcpRelationshipListRequestSchema.TryDecode(payload, limits, out TcpRelationshipListRequest? relationshipListRequest), relationshipListRequest),
            PacketCommand.RelationshipListResponse =>
                FromStatus(TcpRelationshipListResponseSchema.TryDecode(payload, limits, out TcpRelationshipListResponse? relationshipListResponse), relationshipListResponse),
            PacketCommand.TypingNotify =>
                FromStatus(TcpTypingNotifySchema.TryDecode(payload, limits, out TcpTypingNotify? typingNotify), typingNotify),
            PacketCommand.TypingUpdate =>
                FromStatus(TcpTypingUpdateSchema.TryDecode(payload, limits, out TcpTypingUpdate? typingUpdate), typingUpdate),
            PacketCommand.PresenceQuery =>
                FromStatus(TcpPresenceQueryRequestSchema.TryDecode(payload, limits, out TcpPresenceQueryRequest? presenceQueryRequest), presenceQueryRequest),
            PacketCommand.PresenceSnapshot =>
                FromStatus(TcpPresenceSnapshotResponseSchema.TryDecode(payload, limits, out TcpPresenceSnapshotResponse? presenceSnapshot), presenceSnapshot),
            PacketCommand.PresenceChanged =>
                FromStatus(TcpPresenceChangedSchema.TryDecode(payload, limits, out TcpPresenceChanged? presenceChanged), presenceChanged),
            PacketCommand.PresenceUnwatch =>
                FromStatus(TcpPresenceUnwatchRequestSchema.TryDecode(payload, limits, out TcpPresenceUnwatchRequest? presenceUnwatchRequest), presenceUnwatchRequest),
            PacketCommand.RegisterPushTokenRequest =>
                FromStatus(TcpRegisterPushTokenRequestSchema.TryDecode(payload, limits, out TcpRegisterPushTokenRequest? registerPushTokenRequest), registerPushTokenRequest),
            PacketCommand.RegisterPushTokenResponse =>
                FromStatus(TcpRegisterPushTokenResponseSchema.TryDecode(payload, limits, out TcpRegisterPushTokenResponse? registerPushTokenResponse), registerPushTokenResponse),
            PacketCommand.UnregisterPushTokenRequest =>
                FromStatus(TcpUnregisterPushTokenRequestSchema.TryDecode(payload, limits, out TcpUnregisterPushTokenRequest? unregisterPushTokenRequest), unregisterPushTokenRequest),
            PacketCommand.UnregisterPushTokenResponse =>
                FromStatus(TcpUnregisterPushTokenResponseSchema.TryDecode(payload, limits, out TcpUnregisterPushTokenResponse? unregisterPushTokenResponse), unregisterPushTokenResponse),
            PacketCommand.CreateGroupRequest =>
                FromStatus(TcpCreateGroupRequestSchema.TryDecode(payload, limits, out TcpCreateGroupRequest? createGroupRequest), createGroupRequest),
            PacketCommand.CreateGroupResponse =>
                FromStatus(TcpCreateGroupResponseSchema.TryDecode(payload, limits, out TcpCreateGroupResponse? createGroupResponse), createGroupResponse),
            PacketCommand.AddGroupMembersRequest =>
                FromStatus(TcpAddGroupMembersRequestSchema.TryDecode(payload, limits, out TcpAddGroupMembersRequest? addGroupMembersRequest), addGroupMembersRequest),
            PacketCommand.AddGroupMembersResponse =>
                FromStatus(TcpAddGroupMembersResponseSchema.TryDecode(payload, limits, out TcpAddGroupMembersResponse? addGroupMembersResponse), addGroupMembersResponse),
            PacketCommand.RemoveGroupMemberRequest =>
                FromStatus(TcpRemoveGroupMemberRequestSchema.TryDecode(payload, limits, out TcpRemoveGroupMemberRequest? removeGroupMemberRequest), removeGroupMemberRequest),
            PacketCommand.RemoveGroupMemberResponse =>
                FromStatus(TcpRemoveGroupMemberResponseSchema.TryDecode(payload, limits, out TcpRemoveGroupMemberResponse? removeGroupMemberResponse), removeGroupMemberResponse),
            PacketCommand.LeaveGroupRequest =>
                FromStatus(TcpLeaveGroupRequestSchema.TryDecode(payload, limits, out TcpLeaveGroupRequest? leaveGroupRequest), leaveGroupRequest),
            PacketCommand.LeaveGroupResponse =>
                FromStatus(TcpLeaveGroupResponseSchema.TryDecode(payload, limits, out TcpLeaveGroupResponse? leaveGroupResponse), leaveGroupResponse),
            PacketCommand.ChangeMemberRoleRequest =>
                FromStatus(TcpChangeMemberRoleRequestSchema.TryDecode(payload, limits, out TcpChangeMemberRoleRequest? changeMemberRoleRequest), changeMemberRoleRequest),
            PacketCommand.ChangeMemberRoleResponse =>
                FromStatus(TcpChangeMemberRoleResponseSchema.TryDecode(payload, limits, out TcpChangeMemberRoleResponse? changeMemberRoleResponse), changeMemberRoleResponse),
            PacketCommand.ListGroupMembersRequest =>
                FromStatus(TcpListGroupMembersRequestSchema.TryDecode(payload, limits, out TcpListGroupMembersRequest? listGroupMembersRequest), listGroupMembersRequest),
            PacketCommand.ListGroupMembersResponse =>
                FromStatus(TcpListGroupMembersResponseSchema.TryDecode(payload, limits, out TcpListGroupMembersResponse? listGroupMembersResponse), listGroupMembersResponse),
            PacketCommand.MemberJoined =>
                FromStatus(TcpMemberJoinedUpdateSchema.TryDecode(payload, limits, out TcpMemberJoinedUpdate? memberJoined), memberJoined),
            PacketCommand.MemberLeft =>
                FromStatus(TcpMemberLeftUpdateSchema.TryDecode(payload, limits, out TcpMemberLeftUpdate? memberLeft), memberLeft),
            PacketCommand.MemberRemoved =>
                FromStatus(TcpMemberRemovedUpdateSchema.TryDecode(payload, limits, out TcpMemberRemovedUpdate? memberRemoved), memberRemoved),
            PacketCommand.RoleChanged =>
                FromStatus(TcpRoleChangedUpdateSchema.TryDecode(payload, limits, out TcpRoleChangedUpdate? roleChanged), roleChanged),
            PacketCommand.MembersAddedUpdate =>
                FromStatus(TcpMembersAddedUpdateSchema.TryDecode(payload, limits, out TcpMembersAddedUpdate? membersAdded), membersAdded),
            PacketCommand.ConversationDissolvedUpdate =>
                FromStatus(TcpConversationDissolvedUpdateSchema.TryDecode(payload, limits, out TcpConversationDissolvedUpdate? conversationDissolved), conversationDissolved),
            PacketCommand.DissolveGroupRequest =>
                FromStatus(TcpDissolveGroupRequestSchema.TryDecode(payload, limits, out TcpDissolveGroupRequest? dissolveGroupRequest), dissolveGroupRequest),
            PacketCommand.DissolveGroupResponse =>
                FromStatus(TcpDissolveGroupResponseSchema.TryDecode(payload, limits, out TcpDissolveGroupResponse? dissolveGroupResponse), dissolveGroupResponse),
            _ => TcpBinaryWireDecode.NotCovered
        };
    }

    /// <summary>
    /// Dispatches a segmented frame payload without coalescing it. Uncovered commands and
    /// malformed covered payloads fail closed.
    /// </summary>
    public static TcpBinaryWireDecode TryDecode(
        PacketCommand command,
        in ReadOnlySequence<byte> payload,
        BinaryLimits limits)
    {
        return command switch
        {
            PacketCommand.ClientHello =>
                FromStatus(ClientHelloSchema.TryDecode(in payload, limits, out ClientHello? clientHello), clientHello),
            PacketCommand.ServerHello =>
                FromStatus(ServerHelloSchema.TryDecode(in payload, limits, out ServerHello? serverHello), serverHello),
            PacketCommand.GoAway =>
                FromStatus(GoAwaySchema.TryDecode(in payload, limits, out GoAway? goAway), goAway),
            PacketCommand.ResumeResponse =>
                FromStatus(ResumeResponseSchema.TryDecode(in payload, limits, out ResumeResponse? resumeResponse), resumeResponse),
            PacketCommand.Error =>
                FromStatus(ProtocolErrorFrameSchema.TryDecode(in payload, limits, out ProtocolErrorFrame? frame), frame),
            PacketCommand.MessageHistoryRequest =>
                FromStatus(MessageHistoryRequestSchema.TryDecode(in payload, limits, out MessageHistoryRequest? request), request),
            PacketCommand.MessageHistoryPage =>
                FromStatus(MessageHistoryResponseSchema.TryDecode(in payload, limits, out MessageHistoryResponse? response), response),
            PacketCommand.AuthenticationRequest =>
                FromStatus(AuthenticationRequestSchema.TryDecode(in payload, limits, out AuthenticationRequest? authRequest), authRequest),
            PacketCommand.AuthenticationResponse =>
                FromStatus(AuthenticationResponseSchema.TryDecode(in payload, limits, out AuthenticationResponse? authResponse), authResponse),
            PacketCommand.Heartbeat =>
                DecodeEmptyFrame(in payload, HeartbeatMarker),
            PacketCommand.HeartbeatAcknowledgement =>
                DecodeEmptyFrame(in payload, HeartbeatAcknowledgementMarker),
            PacketCommand.ChatMessage =>
                FromStatus(ChatMessageSchema.TryDecode(in payload, limits, out ChatMessage? chatMessage), chatMessage),
            PacketCommand.MessageAcknowledgement =>
                FromStatus(MessageAcknowledgementSchema.TryDecode(in payload, limits, out MessageAcknowledgement? messageAcknowledgement), messageAcknowledgement),
            PacketCommand.MessageReceipt =>
                FromStatus(MessageReceiptSchema.TryDecode(in payload, limits, out MessageReceipt? messageReceipt), messageReceipt),
            PacketCommand.MessageReceiptAcknowledgement =>
                FromStatus(MessageReceiptAcknowledgementSchema.TryDecode(in payload, limits, out MessageReceiptAcknowledgement? receiptAcknowledgement), receiptAcknowledgement),
            PacketCommand.MessageReceiptUpdated =>
                FromStatus(MessageReceiptUpdatedSchema.TryDecode(in payload, limits, out MessageReceiptUpdated? receiptUpdated), receiptUpdated),
            PacketCommand.AttachmentLifecycleChanged =>
                FromStatus(AttachmentLifecycleChangedSchema.TryDecode(in payload, limits, out AttachmentLifecycleChanged? lifecycle), lifecycle),
            PacketCommand.AttachmentFinalizeRequest =>
                FromStatus(AttachmentFinalizeRequestSchema.TryDecode(in payload, limits, out AttachmentFinalizeRequest? finalizeRequest), finalizeRequest),
            PacketCommand.AttachmentFinalizeResponse =>
                FromStatus(AttachmentFinalizeResponseSchema.TryDecode(in payload, limits, out AttachmentFinalizeResponse? finalizeResponse), finalizeResponse),
            PacketCommand.AttachmentDownloadAuthorizeRequest =>
                FromStatus(AttachmentDownloadAuthorizeRequestSchema.TryDecode(in payload, limits, out AttachmentDownloadAuthorizeRequest? downloadAuthorizeRequest), downloadAuthorizeRequest),
            PacketCommand.AttachmentDownloadAuthorizeResponse =>
                FromStatus(AttachmentDownloadAuthorizeResponseSchema.TryDecode(in payload, limits, out AttachmentDownloadAuthorizeResponse? downloadAuthorizeResponse), downloadAuthorizeResponse),
            PacketCommand.CallCommandRequest =>
                FromStatus(TcpCallCommandRequestSchema.TryDecode(in payload, limits, out TcpCallCommandRequest? callCommandRequest), callCommandRequest),
            PacketCommand.CallCommandResponse =>
                FromStatus(TcpCallCommandResponseSchema.TryDecode(in payload, limits, out TcpCallCommandResponse? callCommandResponse), callCommandResponse),
            PacketCommand.CallSignal =>
                FromStatus(TcpCallSignalSchema.TryDecode(in payload, limits, out TcpCallSignal? callSignal), callSignal),
            PacketCommand.MessageEditRequest =>
                FromStatus(MessageEditRequestSchema.TryDecode(in payload, limits, out MessageEditRequest? editRequest), editRequest),
            PacketCommand.MessageEditAck =>
                FromStatus(MessageEditAcknowledgementSchema.TryDecode(in payload, limits, out MessageEditAcknowledgement? editAck), editAck),
            PacketCommand.MessageEdited =>
                FromStatus(MessageEditedUpdateSchema.TryDecode(in payload, limits, out MessageEditedUpdate? edited), edited),
            PacketCommand.MessageRecallRequest =>
                FromStatus(MessageRecallRequestSchema.TryDecode(in payload, limits, out MessageRecallRequest? recallRequest), recallRequest),
            PacketCommand.MessageRecallAck =>
                FromStatus(MessageRecallAcknowledgementSchema.TryDecode(in payload, limits, out MessageRecallAcknowledgement? recallAck), recallAck),
            PacketCommand.MessageRecalled =>
                FromStatus(MessageRecalledUpdateSchema.TryDecode(in payload, limits, out MessageRecalledUpdate? recalled), recalled),
            PacketCommand.AddReactionRequest =>
                FromStatus(AddReactionRequestSchema.TryDecode(in payload, limits, out AddReactionRequest? addReactionRequest), addReactionRequest),
            PacketCommand.AddReactionAck =>
                FromStatus(AddReactionAcknowledgementSchema.TryDecode(in payload, limits, out AddReactionAcknowledgement? addReactionAck), addReactionAck),
            PacketCommand.ReactionAdded =>
                FromStatus(ReactionAddedUpdateSchema.TryDecode(in payload, limits, out ReactionAddedUpdate? reactionAdded), reactionAdded),
            PacketCommand.RemoveReactionRequest =>
                FromStatus(RemoveReactionRequestSchema.TryDecode(in payload, limits, out RemoveReactionRequest? removeReactionRequest), removeReactionRequest),
            PacketCommand.RemoveReactionAck =>
                FromStatus(RemoveReactionAcknowledgementSchema.TryDecode(in payload, limits, out RemoveReactionAcknowledgement? removeReactionAck), removeReactionAck),
            PacketCommand.ReactionRemoved =>
                FromStatus(ReactionRemovedUpdateSchema.TryDecode(in payload, limits, out ReactionRemovedUpdate? reactionRemoved), reactionRemoved),
            PacketCommand.ConversationListRequest =>
                FromStatus(ConversationListRequestSchema.TryDecode(in payload, limits, out ConversationListRequest? conversationListRequest), conversationListRequest),
            PacketCommand.ConversationListPage =>
                FromStatus(ConversationListPageSchema.TryDecode(in payload, limits, out ConversationListPage? conversationListPage), conversationListPage),
            PacketCommand.ConversationMarkReadRequest =>
                FromStatus(ConversationMarkReadRequestSchema.TryDecode(in payload, limits, out ConversationMarkReadRequest? markReadRequest), markReadRequest),
            PacketCommand.ConversationMarkReadResponse =>
                FromStatus(ConversationMarkReadResponseSchema.TryDecode(in payload, limits, out ConversationMarkReadResponse? markReadResponse), markReadResponse),
            PacketCommand.ConversationChanged =>
                FromStatus(ConversationChangedUpdateSchema.TryDecode(in payload, limits, out ConversationChangedUpdate? conversationChanged), conversationChanged),
            PacketCommand.UnreadCountChanged =>
                FromStatus(UnreadCountChangedSchema.TryDecode(in payload, limits, out UnreadCountChanged? unreadCountChanged), unreadCountChanged),
            PacketCommand.SyncBootstrapRequest =>
                FromStatus(SyncBootstrapRequestSchema.TryDecode(in payload, limits, out SyncBootstrapRequest? syncBootstrapRequest), syncBootstrapRequest),
            PacketCommand.SyncBootstrapResponse =>
                FromStatus(SyncBootstrapResponseSchema.TryDecode(in payload, limits, out SyncBootstrapResponse? syncBootstrapResponse), syncBootstrapResponse),
            PacketCommand.ConversationSetPrefsRequest =>
                FromStatus(ConversationSetPrefsRequestSchema.TryDecode(in payload, limits, out ConversationSetPrefsRequest? setPrefsRequest), setPrefsRequest),
            PacketCommand.ConversationSetPrefsResponse =>
                FromStatus(ConversationSetPrefsResponseSchema.TryDecode(in payload, limits, out ConversationSetPrefsResponse? setPrefsResponse), setPrefsResponse),
            PacketCommand.ConversationRead =>
                FromStatus(ConversationReadUpdateSchema.TryDecode(in payload, limits, out ConversationReadUpdate? conversationRead), conversationRead),
            PacketCommand.MessageReadReceiptQueryRequest =>
                FromStatus(MessageReadReceiptQueryRequestSchema.TryDecode(in payload, limits, out MessageReadReceiptQueryRequest? readReceiptQueryRequest), readReceiptQueryRequest),
            PacketCommand.MessageReadReceiptQueryResponse =>
                FromStatus(MessageReadReceiptQueryResponseSchema.TryDecode(in payload, limits, out MessageReadReceiptQueryResponse? readReceiptQueryResponse), readReceiptQueryResponse),
            PacketCommand.RelationshipListChanged =>
                FromStatus(TcpRelationshipListChangedUpdateSchema.TryDecode(in payload, limits, out TcpRelationshipListChangedUpdate? relationshipListChanged), relationshipListChanged),
            PacketCommand.RelationshipCommandRequest =>
                FromStatus(TcpRelationshipCommandRequestSchema.TryDecode(in payload, limits, out TcpRelationshipCommandRequest? relationshipCommandRequest), relationshipCommandRequest),
            PacketCommand.RelationshipCommandResponse =>
                FromStatus(TcpRelationshipCommandResponseSchema.TryDecode(in payload, limits, out TcpRelationshipCommandResponse? relationshipCommandResponse), relationshipCommandResponse),
            PacketCommand.RelationshipListRequest =>
                FromStatus(TcpRelationshipListRequestSchema.TryDecode(in payload, limits, out TcpRelationshipListRequest? relationshipListRequest), relationshipListRequest),
            PacketCommand.RelationshipListResponse =>
                FromStatus(TcpRelationshipListResponseSchema.TryDecode(in payload, limits, out TcpRelationshipListResponse? relationshipListResponse), relationshipListResponse),
            PacketCommand.TypingNotify =>
                FromStatus(TcpTypingNotifySchema.TryDecode(in payload, limits, out TcpTypingNotify? typingNotify), typingNotify),
            PacketCommand.TypingUpdate =>
                FromStatus(TcpTypingUpdateSchema.TryDecode(in payload, limits, out TcpTypingUpdate? typingUpdate), typingUpdate),
            PacketCommand.PresenceQuery =>
                FromStatus(TcpPresenceQueryRequestSchema.TryDecode(in payload, limits, out TcpPresenceQueryRequest? presenceQueryRequest), presenceQueryRequest),
            PacketCommand.PresenceSnapshot =>
                FromStatus(TcpPresenceSnapshotResponseSchema.TryDecode(in payload, limits, out TcpPresenceSnapshotResponse? presenceSnapshot), presenceSnapshot),
            PacketCommand.PresenceChanged =>
                FromStatus(TcpPresenceChangedSchema.TryDecode(in payload, limits, out TcpPresenceChanged? presenceChanged), presenceChanged),
            PacketCommand.PresenceUnwatch =>
                FromStatus(TcpPresenceUnwatchRequestSchema.TryDecode(in payload, limits, out TcpPresenceUnwatchRequest? presenceUnwatchRequest), presenceUnwatchRequest),
            PacketCommand.RegisterPushTokenRequest =>
                FromStatus(TcpRegisterPushTokenRequestSchema.TryDecode(in payload, limits, out TcpRegisterPushTokenRequest? registerPushTokenRequest), registerPushTokenRequest),
            PacketCommand.RegisterPushTokenResponse =>
                FromStatus(TcpRegisterPushTokenResponseSchema.TryDecode(in payload, limits, out TcpRegisterPushTokenResponse? registerPushTokenResponse), registerPushTokenResponse),
            PacketCommand.UnregisterPushTokenRequest =>
                FromStatus(TcpUnregisterPushTokenRequestSchema.TryDecode(in payload, limits, out TcpUnregisterPushTokenRequest? unregisterPushTokenRequest), unregisterPushTokenRequest),
            PacketCommand.UnregisterPushTokenResponse =>
                FromStatus(TcpUnregisterPushTokenResponseSchema.TryDecode(in payload, limits, out TcpUnregisterPushTokenResponse? unregisterPushTokenResponse), unregisterPushTokenResponse),
            PacketCommand.CreateGroupRequest =>
                FromStatus(TcpCreateGroupRequestSchema.TryDecode(in payload, limits, out TcpCreateGroupRequest? createGroupRequest), createGroupRequest),
            PacketCommand.CreateGroupResponse =>
                FromStatus(TcpCreateGroupResponseSchema.TryDecode(in payload, limits, out TcpCreateGroupResponse? createGroupResponse), createGroupResponse),
            PacketCommand.AddGroupMembersRequest =>
                FromStatus(TcpAddGroupMembersRequestSchema.TryDecode(in payload, limits, out TcpAddGroupMembersRequest? addGroupMembersRequest), addGroupMembersRequest),
            PacketCommand.AddGroupMembersResponse =>
                FromStatus(TcpAddGroupMembersResponseSchema.TryDecode(in payload, limits, out TcpAddGroupMembersResponse? addGroupMembersResponse), addGroupMembersResponse),
            PacketCommand.RemoveGroupMemberRequest =>
                FromStatus(TcpRemoveGroupMemberRequestSchema.TryDecode(in payload, limits, out TcpRemoveGroupMemberRequest? removeGroupMemberRequest), removeGroupMemberRequest),
            PacketCommand.RemoveGroupMemberResponse =>
                FromStatus(TcpRemoveGroupMemberResponseSchema.TryDecode(in payload, limits, out TcpRemoveGroupMemberResponse? removeGroupMemberResponse), removeGroupMemberResponse),
            PacketCommand.LeaveGroupRequest =>
                FromStatus(TcpLeaveGroupRequestSchema.TryDecode(in payload, limits, out TcpLeaveGroupRequest? leaveGroupRequest), leaveGroupRequest),
            PacketCommand.LeaveGroupResponse =>
                FromStatus(TcpLeaveGroupResponseSchema.TryDecode(in payload, limits, out TcpLeaveGroupResponse? leaveGroupResponse), leaveGroupResponse),
            PacketCommand.ChangeMemberRoleRequest =>
                FromStatus(TcpChangeMemberRoleRequestSchema.TryDecode(in payload, limits, out TcpChangeMemberRoleRequest? changeMemberRoleRequest), changeMemberRoleRequest),
            PacketCommand.ChangeMemberRoleResponse =>
                FromStatus(TcpChangeMemberRoleResponseSchema.TryDecode(in payload, limits, out TcpChangeMemberRoleResponse? changeMemberRoleResponse), changeMemberRoleResponse),
            PacketCommand.ListGroupMembersRequest =>
                FromStatus(TcpListGroupMembersRequestSchema.TryDecode(in payload, limits, out TcpListGroupMembersRequest? listGroupMembersRequest), listGroupMembersRequest),
            PacketCommand.ListGroupMembersResponse =>
                FromStatus(TcpListGroupMembersResponseSchema.TryDecode(in payload, limits, out TcpListGroupMembersResponse? listGroupMembersResponse), listGroupMembersResponse),
            PacketCommand.MemberJoined =>
                FromStatus(TcpMemberJoinedUpdateSchema.TryDecode(in payload, limits, out TcpMemberJoinedUpdate? memberJoined), memberJoined),
            PacketCommand.MemberLeft =>
                FromStatus(TcpMemberLeftUpdateSchema.TryDecode(in payload, limits, out TcpMemberLeftUpdate? memberLeft), memberLeft),
            PacketCommand.MemberRemoved =>
                FromStatus(TcpMemberRemovedUpdateSchema.TryDecode(in payload, limits, out TcpMemberRemovedUpdate? memberRemoved), memberRemoved),
            PacketCommand.RoleChanged =>
                FromStatus(TcpRoleChangedUpdateSchema.TryDecode(in payload, limits, out TcpRoleChangedUpdate? roleChanged), roleChanged),
            PacketCommand.MembersAddedUpdate =>
                FromStatus(TcpMembersAddedUpdateSchema.TryDecode(in payload, limits, out TcpMembersAddedUpdate? membersAdded), membersAdded),
            PacketCommand.ConversationDissolvedUpdate =>
                FromStatus(TcpConversationDissolvedUpdateSchema.TryDecode(in payload, limits, out TcpConversationDissolvedUpdate? conversationDissolved), conversationDissolved),
            PacketCommand.DissolveGroupRequest =>
                FromStatus(TcpDissolveGroupRequestSchema.TryDecode(in payload, limits, out TcpDissolveGroupRequest? dissolveGroupRequest), dissolveGroupRequest),
            PacketCommand.DissolveGroupResponse =>
                FromStatus(TcpDissolveGroupResponseSchema.TryDecode(in payload, limits, out TcpDissolveGroupResponse? dissolveGroupResponse), dissolveGroupResponse),
            _ => TcpBinaryWireDecode.NotCovered
        };
    }

    private static TcpBinaryWireDecode FromStatus(BinaryStatus status, object? value)
    {
        if (status == BinaryStatus.Done && value is not null)
        {
            return TcpBinaryWireDecode.Success(value);
        }

        return TcpBinaryWireDecode.Failure(status);
    }

    private static TcpBinaryWireDecode DecodeEmptyFrame(ReadOnlySpan<byte> payload, object marker) =>
        payload.Length == 0
            ? TcpBinaryWireDecode.Success(marker)
            : TcpBinaryWireDecode.Failure(BinaryStatus.TrailingData);

    private static TcpBinaryWireDecode DecodeEmptyFrame(in ReadOnlySequence<byte> payload, object marker) =>
        payload.Length == 0
            ? TcpBinaryWireDecode.Success(marker)
            : TcpBinaryWireDecode.Failure(BinaryStatus.TrailingData);
}