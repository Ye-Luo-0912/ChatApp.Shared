using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;

namespace ChatApp.Shared.Protocol.Tcp.Binary.Schemas;

/// <summary>
/// Stable outcome of a wire-encoder dispatch. When <see cref="Status"/> is
/// <see cref="TcpBinaryWireEncodeStatus.Encoded"/>, exactly <see cref="Written"/> bytes of
/// <c>destination</c> hold the canonical payload; otherwise nothing was written and the caller
/// must fail closed.
/// </summary>
public enum TcpBinaryWireEncodeStatus : byte
{
    /// <summary>The value's type is part of the negotiated subset and its payload was encoded.</summary>
    Encoded = 0,

    /// <summary>The value's type has no binary-v1 schema; the caller must fail closed.</summary>
    SchemaNotCovered,

    /// <summary>A covered value failed to encode; see <see cref="EncodeStatus"/>. Nothing was written.</summary>
    EncodeFailure,
}

/// <summary>Result of dispatching a value to the type-to-schema encode registry.</summary>
public readonly struct TcpBinaryWireEncode
{
    public static TcpBinaryWireEncode NotCovered { get; } =
        new(TcpBinaryWireEncodeStatus.SchemaNotCovered, BinaryStatus.Done, 0);

    public static TcpBinaryWireEncode Failure(BinaryStatus encodeStatus) =>
        new(TcpBinaryWireEncodeStatus.EncodeFailure, encodeStatus, 0);

    public static TcpBinaryWireEncode Success(int written) =>
        new(TcpBinaryWireEncodeStatus.Encoded, BinaryStatus.Done, written);

    private TcpBinaryWireEncode(TcpBinaryWireEncodeStatus status, BinaryStatus encodeStatus, int written)
    {
        Status = status;
        EncodeStatus = encodeStatus;
        Written = written;
    }

    public TcpBinaryWireEncodeStatus Status { get; }

    /// <summary>Populated only when <see cref="Status"/> is <see cref="TcpBinaryWireEncodeStatus.EncodeFailure"/>.</summary>
    public BinaryStatus EncodeStatus { get; }

    /// <summary>Payload bytes written to the destination; zero unless <see cref="Status"/> is <see cref="TcpBinaryWireEncodeStatus.Encoded"/>.</summary>
    public int Written { get; }
}

/// <summary>
/// The type-to-schema encode registry for the negotiated <c>chatapp-bin-v1</c> subset. It is the
/// encode-side counterpart of <see cref="TcpBinaryWireCodec"/>: one covered schema DTO type per
/// frame payload, hand-written dispatch, no reflection and no runtime registry. Uncovered types,
/// null values and any per-field limit violation fail closed without writing destination bytes.
/// The two payload-less keep-alive frames encode as empty payloads like on the decode side.
/// </summary>
public static class TcpBinaryWireEncoder
{
    /// <summary>
    /// Encodes <paramref name="value"/> into <paramref name="destination"/> using the schema for
    /// its concrete DTO type. Uncovered types and malformed/over-limit covered values fail closed.
    /// </summary>
    public static TcpBinaryWireEncode TryEncode<T>(T? value, Span<byte> destination, BinaryLimits limits)
        where T : class
    {
        if (value is null)
        {
            return TcpBinaryWireEncode.Failure(BinaryStatus.MissingRequiredField);
        }

        if (!TryEncodeDispatch(value, destination, limits, out var status, out var written))
        {
            return TcpBinaryWireEncode.NotCovered;
        }

        return status == BinaryStatus.Done
            ? TcpBinaryWireEncode.Success(written)
            : TcpBinaryWireEncode.Failure(status);
    }

    private static bool TryEncodeDispatch<T>(T value, Span<byte> destination, BinaryLimits limits, out BinaryStatus status, out int written)
        where T : class
    {
        written = 0;
        switch (value)
        {
            case ClientHello v:
                status = ClientHelloSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ServerHello v:
                status = ServerHelloSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case GoAway v:
                status = GoAwaySchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ResumeResponse v:
                status = ResumeResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ProtocolErrorFrame v:
                status = ProtocolErrorFrameSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageHistoryRequest v:
                status = MessageHistoryRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageHistoryResponse v:
                status = MessageHistoryResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case AuthenticationRequest v:
                status = AuthenticationRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case AuthenticationResponse v:
                status = AuthenticationResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case Heartbeat:
            case HeartbeatAcknowledgement:
                status = BinaryStatus.Done;
                return true;
            case ChatMessage v:
                status = ChatMessageSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageAcknowledgement v:
                status = MessageAcknowledgementSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageReceipt v:
                status = MessageReceiptSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageReceiptAcknowledgement v:
                status = MessageReceiptAcknowledgementSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageReceiptUpdated v:
                status = MessageReceiptUpdatedSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case AttachmentLifecycleChanged v:
                status = AttachmentLifecycleChangedSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case AttachmentFinalizeRequest v:
                status = AttachmentFinalizeRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case AttachmentFinalizeResponse v:
                status = AttachmentFinalizeResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case AttachmentDownloadAuthorizeRequest v:
                status = AttachmentDownloadAuthorizeRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case AttachmentDownloadAuthorizeResponse v:
                status = AttachmentDownloadAuthorizeResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpCallCommandRequest v:
                status = TcpCallCommandRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpCallCommandResponse v:
                status = TcpCallCommandResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpCallSignal v:
                status = TcpCallSignalSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageEditRequest v:
                status = MessageEditRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageEditAcknowledgement v:
                status = MessageEditAcknowledgementSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageEditedUpdate v:
                status = MessageEditedUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageRecallRequest v:
                status = MessageRecallRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageRecallAcknowledgement v:
                status = MessageRecallAcknowledgementSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageRecalledUpdate v:
                status = MessageRecalledUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case AddReactionRequest v:
                status = AddReactionRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case AddReactionAcknowledgement v:
                status = AddReactionAcknowledgementSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ReactionAddedUpdate v:
                status = ReactionAddedUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case RemoveReactionRequest v:
                status = RemoveReactionRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case RemoveReactionAcknowledgement v:
                status = RemoveReactionAcknowledgementSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ReactionRemovedUpdate v:
                status = ReactionRemovedUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ConversationListRequest v:
                status = ConversationListRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ConversationListPage v:
                status = ConversationListPageSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ConversationMarkReadRequest v:
                status = ConversationMarkReadRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ConversationMarkReadResponse v:
                status = ConversationMarkReadResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ConversationChangedUpdate v:
                status = ConversationChangedUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case UnreadCountChanged v:
                status = UnreadCountChangedSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case SyncBootstrapRequest v:
                status = SyncBootstrapRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case SyncBootstrapResponse v:
                status = SyncBootstrapResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ConversationSetPrefsRequest v:
                status = ConversationSetPrefsRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ConversationSetPrefsResponse v:
                status = ConversationSetPrefsResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case ConversationReadUpdate v:
                status = ConversationReadUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageReadReceiptQueryRequest v:
                status = MessageReadReceiptQueryRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case MessageReadReceiptQueryResponse v:
                status = MessageReadReceiptQueryResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpRelationshipListChangedUpdate v:
                status = TcpRelationshipListChangedUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpRelationshipCommandRequest v:
                status = TcpRelationshipCommandRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpRelationshipCommandResponse v:
                status = TcpRelationshipCommandResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpRelationshipListRequest v:
                status = TcpRelationshipListRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpRelationshipListResponse v:
                status = TcpRelationshipListResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpTypingNotify v:
                status = TcpTypingNotifySchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpTypingUpdate v:
                status = TcpTypingUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpPresenceQueryRequest v:
                status = TcpPresenceQueryRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpPresenceSnapshotResponse v:
                status = TcpPresenceSnapshotResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpPresenceChanged v:
                status = TcpPresenceChangedSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpPresenceUnwatchRequest v:
                status = TcpPresenceUnwatchRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpRegisterPushTokenRequest v:
                status = TcpRegisterPushTokenRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpRegisterPushTokenResponse v:
                status = TcpRegisterPushTokenResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpUnregisterPushTokenRequest v:
                status = TcpUnregisterPushTokenRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpUnregisterPushTokenResponse v:
                status = TcpUnregisterPushTokenResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpCreateGroupRequest v:
                status = TcpCreateGroupRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpCreateGroupResponse v:
                status = TcpCreateGroupResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpAddGroupMembersRequest v:
                status = TcpAddGroupMembersRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpAddGroupMembersResponse v:
                status = TcpAddGroupMembersResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpRemoveGroupMemberRequest v:
                status = TcpRemoveGroupMemberRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpRemoveGroupMemberResponse v:
                status = TcpRemoveGroupMemberResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpLeaveGroupRequest v:
                status = TcpLeaveGroupRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpLeaveGroupResponse v:
                status = TcpLeaveGroupResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpChangeMemberRoleRequest v:
                status = TcpChangeMemberRoleRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpChangeMemberRoleResponse v:
                status = TcpChangeMemberRoleResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpListGroupMembersRequest v:
                status = TcpListGroupMembersRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpListGroupMembersResponse v:
                status = TcpListGroupMembersResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpMemberJoinedUpdate v:
                status = TcpMemberJoinedUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpMemberLeftUpdate v:
                status = TcpMemberLeftUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpMemberRemovedUpdate v:
                status = TcpMemberRemovedUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpRoleChangedUpdate v:
                status = TcpRoleChangedUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpMembersAddedUpdate v:
                status = TcpMembersAddedUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpConversationDissolvedUpdate v:
                status = TcpConversationDissolvedUpdateSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpDissolveGroupRequest v:
                status = TcpDissolveGroupRequestSchema.TryEncode(in v, destination, limits, out written);
                return true;
            case TcpDissolveGroupResponse v:
                status = TcpDissolveGroupResponseSchema.TryEncode(in v, destination, limits, out written);
                return true;
            default:
                status = BinaryStatus.Done;
                return false;
        }
    }
}
