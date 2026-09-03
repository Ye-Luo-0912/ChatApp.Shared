using System.Buffers;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

namespace ChatApp.Shared.Protocol.Tcp.Binary.Schemas;

/// <summary>
/// Canonical binary-v1 field numbers for the core negotiated-format command subset. These
/// numbers are frozen by the wire goldens established during BIN-SCHEMA-2 and are authoritative;
/// they must never be renumbered or reused across wire-incompatible changes.
/// </summary>
public static class CoreCommandFieldNumbers
{
    public static class ClientHello
    {
        public const int ProtocolVersion = 1;
        public const int FeatureBits = 2;
        public const int InstallationId = 3;
        public const int ClientTimeMs = 4;
        public const int ResumeToken = 5;
        public const int MaxPayloadBytes = 6;
    }

    public static class ServerHello
    {
        public const int ProtocolVersion = 1;
        public const int FeatureBits = 2;
        public const int ServerDeviceId = 3;
        public const int ServerTimeMs = 4;
        public const int HeartbeatIntervalMs = 5;
        public const int MaxPayloadBytes = 6;
        public const int ResumeSupported = 7;
        public const int PayloadFormat = 8;
    }

    public static class GoAway
    {
        public const int RetryAfterMs = 1;
        public const int Reason = 2;
        public const int ServerHint = 3;
    }

    public static class ResumeResponse
    {
        public const int Success = 1;
        public const int FailureKind = 2;
        public const int ResumeToken = 3;
        public const int UserId = 4;
        public const int SessionId = 5;
        public const int DeviceId = 6;
        public const int LastConversationSequence = 7;
        public const int ErrorMessage = 8;
        public const int RetryAfterMs = 9;
    }

    public static class ProtocolErrorFrame
    {
        public const int Code = 1;
        public const int Fatal = 2;
        public const int RetryAfterMs = 3;
        public const int Message = 4;
        public const int OriginCommand = 5;
    }

    public static class MessageHistoryRequest
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
        public const int BeforeReceivedAtMs = 3;
        public const int BeforeMessageId = 4;
        public const int AfterReceivedAtMs = 5;
        public const int AfterMessageId = 6;
        public const int Limit = 7;
    }

    public static class MessageHistoryCursor
    {
        public const int ReceivedAtMs = 1;
        public const int ChangedAtMs = 2;
        public const int MessageId = 3;
    }

    public static class TcpAttachmentRef
    {
        public const int RefVersion = 1;
        public const int AttachmentId = 2;
        public const int FileName = 3;
        public const int ContentType = 4;
        public const int SizeBytes = 5;
        public const int Status = 6;
        public const int DownloadApiHint = 7;
        public const int DownloadToken = 8;
        public const int ThumbnailApiHint = 9;
        public const int IsVoice = 10;
        public const int VoiceCodec = 11;
        public const int VoiceContainer = 12;
        public const int VoiceDurationMs = 13;
        public const int VoiceSampleRateHz = 14;
        public const int VoiceChannels = 15;
    }

    public static class MessageReactionSummary
    {
        public const int Emoji = 1;
        public const int Count = 2;
        public const int ReactedByMe = 3;
    }

    public static class MessageHistoryItem
    {
        public const int MessageId = 1;
        public const int ClientMessageId = 2;
        public const int SenderUserId = 3;
        public const int ReceiverUserId = 4;
        public const int ConversationId = 5;
        public const int Content = 6;
        public const int ReceivedAtMs = 7;
        public const int DeliveredAtMs = 8;
        public const int ReadAtMs = 9;
        public const int RecalledAtMs = 10;
        public const int EditVersion = 11;
        public const int EditedAtMs = 12;
        public const int ChangedAtMs = 13;
        public const int Attachments = 14;
        public const int Reactions = 15;
        public const int ReplyToMessageId = 16;
        public const int ReplyToSenderUserId = 17;
        public const int ReplyToPreview = 18;
        public const int ForwardedFromMessageId = 19;
        public const int ForwardedFromSenderUserId = 20;
        public const int ForwardedFromPreview = 21;
        public const int MentionedUserIds = 22;
        public const int MentionedRoles = 23;
    }

    public static class MessageHistoryResponse
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
        public const int Succeeded = 3;
        public const int ErrorCode = 4;
        public const int ErrorMessage = 5;
        public const int Items = 6;
        public const int NextCursor = 7;
        public const int HasMore = 8;
    }

    public static class AuthenticationRequest
    {
        public const int AccessToken = 1;
        public const int DeviceIdHash = 2;
    }

    public static class AuthenticationResponse
    {
        public const int Success = 1;
        public const int UserId = 2;
        public const int ErrorMessage = 3;
        public const int SessionId = 4;
        public const int DeviceIdHash = 5;
        public const int DeviceId = 6;
        public const int ResumeToken = 7;
    }

    public static class ChatMessage
    {
        public const int ClientMessageId = 1;
        public const int MessageId = 2;
        public const int ConversationId = 3;
        public const int TargetUserId = 4;
        public const int SenderUserId = 5;
        public const int Content = 6;
        public const int SentAtMs = 7;
        public const int AttachmentIds = 8;
        public const int Attachments = 9;
        public const int ReplyToMessageId = 10;
        public const int ReplyToSenderUserId = 11;
        public const int ReplyToPreview = 12;
        public const int ForwardedFromMessageId = 13;
        public const int ForwardedFromSenderUserId = 14;
        public const int ForwardedFromPreview = 15;
        public const int MentionedUserIds = 16;
        public const int MentionedRoles = 17;
    }

    public static class MessageAcknowledgement
    {
        public const int ClientMessageId = 1;
        public const int CommandId = 2;
        public const int Accepted = 3;
        public const int ErrorCode = 4;
        public const int ErrorMessage = 5;
        public const int AcknowledgedAtMs = 6;
    }

    public static class MessageReceipt
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
        public const int LastReadMessageId = 3;
        public const int LastReadAtMs = 4;
        public const int ReaderUserId = 5;
        public const int ReceiverUserId = 6;
    }

    public static class MessageReceiptAcknowledgement
    {
        public const int RequestId = 1;
        public const int Accepted = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
    }

    public static class MessageReceiptUpdated
    {
        public const int ConversationId = 1;
        public const int LastReadMessageId = 2;
        public const int LastReadAtMs = 3;
        public const int ReaderUserId = 4;
    }

    public static class AttachmentLifecycleChanged
    {
        public const int AttachmentId = 1;
        public const int Status = 2;
        public const int OccurredAtMs = 3;
        public const int RejectReason = 4;
        public const int ThumbnailApiHint = 5;
        public const int DownloadToken = 6;
    }

    public static class AttachmentFinalizeRequest
    {
        public const int RequestId = 1;
        public const int AttachmentId = 2;
        public const int SizeBytes = 3;
        public const int ContentHash = 4;
    }

    public static class AttachmentFinalizeResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int AttachmentId = 5;
        public const int Status = 6;
    }

    public static class AttachmentDownloadAuthorizeRequest
    {
        public const int RequestId = 1;
        public const int AttachmentId = 2;
        public const int ConversationId = 3;
    }

    public static class AttachmentDownloadAuthorizeResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int AttachmentId = 5;
        public const int DownloadUrl = 6;
        public const int DownloadToken = 7;
        public const int ExpiresAtMs = 8;
    }

    public static class TcpCallGrant
    {
        public const int CallId = 1;
        public const int CallerUserId = 2;
        public const int CalleeUserId = 3;
        public const int ExpiresAtMs = 4;
        public const int Nonce = 5;
        public const int Signature = 6;
    }

    public static class TcpCallCommandRequest
    {
        public const int RequestId = 1;
        public const int CommandId = 2;
        public const int CallId = 3;
        public const int Type = 4;
        public const int ActorUserId = 5;
        public const int Revision = 6;
        public const int Grant = 7;
        public const int Sdp = 8;
        public const int ClientOccurredAtMs = 9;
    }

    public static class TcpCallSignal
    {
        public const int SignalId = 1;
        public const int CallId = 2;
        public const int FromUserId = 3;
        public const int ToUserId = 4;
        public const int Kind = 5;
        public const int Sdp = 6;
        public const int Revision = 7;
        public const int OccurredAtMs = 8;
    }

    public static class TcpCallCommandResponse
    {
        public const int RequestId = 1;
        public const int CallId = 2;
        public const int Succeeded = 3;
        public const int ErrorCode = 4;
        public const int ErrorMessage = 5;
        public const int State = 6;
        public const int EndReason = 7;
        public const int Revision = 8;
        public const int Replayed = 9;
        public const int SignalToForward = 10;
    }

    public static class MessageEditRequest
    {
        public const int RequestId = 1;
        public const int MessageId = 2;
        public const int Content = 3;
    }

    public static class MessageEditAcknowledgement
    {
        public const int RequestId = 1;
        public const int MessageId = 2;
        public const int Succeeded = 3;
        public const int ErrorCode = 4;
        public const int ErrorMessage = 5;
        public const int ConversationId = 6;
        public const int Content = 7;
        public const int EditVersion = 8;
        public const int EditedAtMs = 9;
    }

    public static class MessageEditedUpdate
    {
        public const int MessageId = 1;
        public const int ConversationId = 2;
        public const int SenderUserId = 3;
        public const int ReceiverUserId = 4;
        public const int Content = 5;
        public const int EditVersion = 6;
        public const int EditedAtMs = 7;
    }

    public static class MessageRecallRequest
    {
        public const int RequestId = 1;
        public const int MessageId = 2;
    }

    public static class MessageRecallAcknowledgement
    {
        public const int RequestId = 1;
        public const int MessageId = 2;
        public const int Succeeded = 3;
        public const int ErrorCode = 4;
        public const int ErrorMessage = 5;
        public const int ConversationId = 6;
        public const int RecalledAtMs = 7;
    }

    public static class MessageRecalledUpdate
    {
        public const int MessageId = 1;
        public const int ConversationId = 2;
        public const int SenderUserId = 3;
        public const int ReceiverUserId = 4;
        public const int RecalledAtMs = 5;
    }

    public static class AddReactionRequest
    {
        public const int RequestId = 1;
        public const int MessageId = 2;
        public const int Emoji = 3;
    }

    public static class AddReactionAcknowledgement
    {
        public const int RequestId = 1;
        public const int MessageId = 2;
        public const int Succeeded = 3;
        public const int ErrorCode = 4;
        public const int ErrorMessage = 5;
        public const int ConversationId = 6;
        public const int Emoji = 7;
        public const int OccurredAtMs = 8;
        public const int EmojiCount = 9;
    }

    public static class ReactionAddedUpdate
    {
        public const int MessageId = 1;
        public const int ConversationId = 2;
        public const int ReactorUserId = 3;
        public const int MessageSenderUserId = 4;
        public const int MessageReceiverUserId = 5;
        public const int Emoji = 6;
        public const int EmojiCount = 7;
        public const int OccurredAtMs = 8;
    }

    public static class RemoveReactionRequest
    {
        public const int RequestId = 1;
        public const int MessageId = 2;
        public const int Emoji = 3;
    }

    public static class RemoveReactionAcknowledgement
    {
        public const int RequestId = 1;
        public const int MessageId = 2;
        public const int Succeeded = 3;
        public const int ErrorCode = 4;
        public const int ErrorMessage = 5;
        public const int ConversationId = 6;
        public const int Emoji = 7;
        public const int OccurredAtMs = 8;
        public const int EmojiCount = 9;
    }

    public static class ReactionRemovedUpdate
    {
        public const int MessageId = 1;
        public const int ConversationId = 2;
        public const int ReactorUserId = 3;
        public const int MessageSenderUserId = 4;
        public const int MessageReceiverUserId = 5;
        public const int Emoji = 6;
        public const int EmojiCount = 7;
        public const int OccurredAtMs = 8;
    }

    public static class ConversationListRequest
    {
        public const int RequestId = 1;
        public const int BeforeIsPinned = 2;
        public const int BeforePinnedAtMs = 3;
        public const int BeforeLastMessageAtMs = 4;
        public const int BeforeConversationId = 5;
        public const int Limit = 6;
    }

    public static class TcpConversationListItem
    {
        public const int ConversationId = 1;
        public const int Type = 2;
        public const int PeerUserId = 3;
        public const int Title = 4;
        public const int LastMessageId = 5;
        public const int LastMessagePreview = 6;
        public const int LastMessageAtMs = 7;
        public const int LastSenderUserId = 8;
        public const int UnreadCount = 9;
        public const int LastReadMessageId = 10;
        public const int LastReadAtMs = 11;
        public const int IsPinned = 12;
        public const int PinnedAtMs = 13;
        public const int IsMuted = 14;
        public const int MutedUntilMs = 15;
    }

    public static class TcpConversationListCursor
    {
        public const int IsPinned = 1;
        public const int PinnedAtMs = 2;
        public const int LastMessageAtMs = 3;
        public const int ConversationId = 4;
    }

    public static class ConversationListPage
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int Items = 5;
        public const int NextCursor = 6;
        public const int HasMore = 7;
    }

    public static class ConversationMarkReadRequest
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
        public const int ReadAtMs = 3;
        public const int ReadMessageId = 4;
    }

    public static class ConversationMarkReadResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ConversationId = 5;
        public const int UnreadCount = 6;
        public const int LastReadMessageId = 7;
        public const int LastReadAtMs = 8;
        public const int Changed = 9;
    }

    public static class ConversationChangedUpdate
    {
        public const int ConversationId = 1;
        public const int Type = 2;
        public const int PeerUserId = 3;
        public const int Title = 4;
        public const int LastMessageId = 5;
        public const int LastMessagePreview = 6;
        public const int LastMessageAtMs = 7;
        public const int LastSenderUserId = 8;
        public const int IsPinned = 9;
        public const int IsMuted = 10;
        public const int MutedUntilMs = 11;
    }

    public static class UnreadCountChanged
    {
        public const int ConversationId = 1;
        public const int UnreadCount = 2;
        public const int LastReadMessageId = 3;
        public const int LastReadAtMs = 4;
    }

    public static class ConversationSetPrefsRequest
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
        public const int Pinned = 3;
        public const int Muted = 4;
        public const int MutedUntilMs = 5;
    }

    public static class ConversationSetPrefsResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ConversationId = 5;
        public const int IsPinned = 6;
        public const int IsMuted = 7;
        public const int MutedUntilMs = 8;
        public const int Changed = 9;
    }

    public static class ConversationReadUpdate
    {
        public const int ConversationId = 1;
        public const int ReaderUserId = 2;
        public const int LastReadMessageId = 3;
        public const int LastReadAtMs = 4;
    }

    public static class MessageReadReceiptItem
    {
        public const int UserId = 1;
        public const int ReadAtMs = 2;
    }

    public static class MessageReadReceiptQueryRequest
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
        public const int MessageId = 3;
        public const int Cursor = 4;
        public const int PageSize = 5;
    }

    public static class MessageReadReceiptQueryResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ConversationId = 5;
        public const int ReadCount = 6;
        public const int TotalMemberCount = 7;
        public const int IsSmallGroup = 8;
        public const int Readers = 9;
        public const int NextCursor = 10;
        public const int HasMore = 11;
    }

    public static class ConversationSyncWatermark
    {
        public const int ConversationId = 1;
        public const int AfterReceivedAtMs = 2;
        public const int AfterMessageId = 3;
    }

    public static class RelationshipSyncWatermark
    {
        public const int ListType = 1;
        public const int AfterSequence = 2;
    }

    public static class ConversationHistoryCatchUp
    {
        public const int ConversationId = 1;
        public const int Items = 2;
        public const int HasMore = 3;
        public const int NextCursor = 4;
    }

    public static class SyncCursorResetRequired
    {
        public const int ConversationId = 1;
        public const int Reason = 2;
        public const int TipMessageId = 3;
        public const int TipReceivedAtMs = 4;
        public const int ClientAfterReceivedAtMs = 5;
        public const int ClientAfterMessageId = 6;
    }

    public static class RelationshipChangeLogEntry
    {
        public const int Operation = 1;
        public const int ResourceId = 2;
        public const int UserId = 3;
        public const int Status = 4;
        public const int Message = 5;
        public const int CreatedAtMs = 6;
        public const int OccurredAtMs = 7;
    }

    public static class RelationshipCatchUp
    {
        public const int ListType = 1;
        public const int Changes = 2;
        public const int HasMore = 3;
        public const int NextCursor = 4;
        public const int NextSequence = 5;
        public const int ResetRequired = 6;
        public const int ErrorCode = 7;
        public const int ErrorMessage = 8;
    }

    public static class SyncBootstrapRequest
    {
        public const int RequestId = 1;
        public const int ListLimit = 2;
        public const int HistoryLimitPerConversation = 3;
        public const int MaxConversationsWithHistory = 4;
        public const int Watermarks = 5;
        public const int RelationshipWatermarks = 6;
        public const int RelationshipListLimit = 7;
    }

    public static class SyncBootstrapResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ServerTimeMs = 5;
        public const int Conversations = 6;
        public const int ConversationsNextCursor = 7;
        public const int ConversationsHasMore = 8;
        public const int CatchUps = 9;
        public const int ResetsRequired = 10;
        public const int RelationshipCatchUps = 11;
    }

    public static class TcpRelationshipListItem
    {
        public const int UserId = 1;
        public const int ResourceId = 2;
        public const int Status = 3;
        public const int Message = 4;
        public const int CreatedAtMs = 5;
    }

    public static class TcpRelationshipListRequest
    {
        public const int RequestId = 1;
        public const int ListType = 2;
        public const int PageSize = 3;
        public const int Cursor = 4;
    }

    public static class TcpRelationshipListResponse
    {
        public const int RequestId = 1;
        public const int ListType = 2;
        public const int Succeeded = 3;
        public const int ErrorCode = 4;
        public const int ErrorMessage = 5;
        public const int ResetRequired = 6;
        public const int Items = 7;
        public const int NextCursor = 8;
        public const int HasMore = 9;
    }

    public static class TcpRelationshipCommandRequest
    {
        public const int RequestId = 1;
        public const int Operation = 2;
        public const int TargetUserId = 3;
        public const int Message = 4;
        public const int RequestIdToRespond = 5;
    }

    public static class TcpRelationshipCommandResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int Operation = 5;
        public const int TargetUserId = 6;
        public const int ResourceId = 7;
    }

    public static class TcpRelationshipListChangedUpdate
    {
        public const int Resource = 1;
        public const int Action = 2;
        public const int ResourceId = 3;
        public const int ActorUserId = 4;
        public const int Message = 5;
        public const int OccurredAtMs = 6;
    }

    public static class TcpTypingNotify
    {
        public const int TargetUserId = 1;
        public const int ConversationId = 2;
        public const int IsTyping = 3;
    }

    public static class TcpTypingUpdate
    {
        public const int SenderUserId = 1;
        public const int ConversationId = 2;
        public const int IsTyping = 3;
    }

    public static class TcpPresenceQueryRequest
    {
        public const int RequestId = 1;
        public const int UserIds = 2;
    }

    public static class TcpPresenceUnwatchRequest
    {
        public const int UserIds = 1;
    }

    public static class TcpPresenceSnapshotItem
    {
        public const int UserId = 1;
        public const int IsOnline = 2;
    }

    public static class TcpPresenceSnapshotResponse
    {
        public const int RequestId = 1;
        public const int Items = 2;
    }

    public static class TcpPresenceChanged
    {
        public const int UserId = 1;
        public const int IsOnline = 2;
    }

    public static class TcpRegisterPushTokenRequest
    {
        public const int RequestId = 1;
        public const int Platform = 2;
        public const int Token = 3;
        public const int AppDeviceLabel = 4;
    }

    public static class TcpRegisterPushTokenResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ActiveTokenCount = 5;
    }

    public static class TcpUnregisterPushTokenRequest
    {
        public const int RequestId = 1;
        public const int Token = 2;
    }

    public static class TcpUnregisterPushTokenResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ActiveTokenCount = 5;
    }

    public static class TcpConversationMemberItem
    {
        public const int UserId = 1;
        public const int Role = 2;
        public const int JoinedAtMs = 3;
    }

    public static class TcpCreateGroupRequest
    {
        public const int RequestId = 1;
        public const int Title = 2;
        public const int MemberUserIds = 3;
    }

    public static class TcpCreateGroupResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ConversationId = 5;
        public const int Title = 6;
        public const int Members = 7;
    }

    public static class TcpAddGroupMembersRequest
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
        public const int MemberUserIds = 3;
    }

    public static class TcpAddGroupMembersResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ConversationId = 5;
        public const int Members = 6;
    }

    public static class TcpRemoveGroupMemberRequest
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
        public const int TargetUserId = 3;
    }

    public static class TcpRemoveGroupMemberResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ConversationId = 5;
    }

    public static class TcpLeaveGroupRequest
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
    }

    public static class TcpLeaveGroupResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ConversationId = 5;
    }

    public static class TcpDissolveGroupRequest
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
    }

    public static class TcpDissolveGroupResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ConversationId = 5;
    }

    public static class TcpChangeMemberRoleRequest
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
        public const int TargetUserId = 3;
        public const int NewRole = 4;
    }

    public static class TcpChangeMemberRoleResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ConversationId = 5;
    }

    public static class TcpListGroupMembersRequest
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
        public const int PageSize = 3;
        public const int Cursor = 4;
    }

    public static class TcpListGroupMembersResponse
    {
        public const int RequestId = 1;
        public const int Succeeded = 2;
        public const int ErrorCode = 3;
        public const int ErrorMessage = 4;
        public const int ConversationId = 5;
        public const int Members = 6;
        public const int NextCursor = 7;
        public const int HasMore = 8;
    }

    public static class TcpMemberJoinedUpdate
    {
        public const int ConversationId = 1;
        public const int UserId = 2;
        public const int Role = 3;
        public const int ActorUserId = 4;
        public const int Title = 5;
        public const int OccurredAtMs = 6;
    }

    public static class TcpMemberLeftUpdate
    {
        public const int ConversationId = 1;
        public const int UserId = 2;
        public const int OccurredAtMs = 3;
    }

    public static class TcpMemberRemovedUpdate
    {
        public const int ConversationId = 1;
        public const int UserId = 2;
        public const int ActorUserId = 3;
        public const int OccurredAtMs = 4;
    }

    public static class TcpRoleChangedUpdate
    {
        public const int ConversationId = 1;
        public const int UserId = 2;
        public const int NewRole = 3;
        public const int PreviousRole = 4;
        public const int ActorUserId = 5;
        public const int OccurredAtMs = 6;
    }

    public static class TcpMembersAddedUpdate
    {
        public const int ConversationId = 1;
        public const int AddedUserIds = 2;
        public const int ActorUserId = 3;
        public const int Title = 4;
        public const int OccurredAtMs = 5;
    }

    public static class TcpConversationDissolvedUpdate
    {
        public const int ConversationId = 1;
        public const int ActorUserId = 2;
        public const int OccurredAtMs = 3;
    }
}

[TcpBinaryContract(typeof(ClientHello))]
[TcpBinaryField(CoreCommandFieldNumbers.ClientHello.ProtocolVersion, nameof(ClientHello.ProtocolVersion))]
[TcpBinaryField(CoreCommandFieldNumbers.ClientHello.FeatureBits, nameof(ClientHello.FeatureBits))]
[TcpBinaryField(CoreCommandFieldNumbers.ClientHello.InstallationId, nameof(ClientHello.InstallationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ClientHello.ClientTimeMs, nameof(ClientHello.ClientTimeMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ClientHello.ResumeToken, nameof(ClientHello.ResumeToken))]
[TcpBinaryField(CoreCommandFieldNumbers.ClientHello.MaxPayloadBytes, nameof(ClientHello.MaxPayloadBytes))]
public static partial class ClientHelloSchema
{
    public static BinaryStatus TryEncode(
        in ClientHello value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ClientHelloSchemaEncoder, ClientHello>(in value, destination, limits, out written);
}

public readonly struct ClientHelloSchemaEncoder : IBinaryEncoder<ClientHelloSchemaEncoder, ClientHello>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ClientHello value)
    {
        writer.WriteUInt32(CoreCommandFieldNumbers.ClientHello.ProtocolVersion, value.ProtocolVersion);
        writer.WriteUInt32(CoreCommandFieldNumbers.ClientHello.FeatureBits, value.FeatureBits);
        if (value.InstallationId is { } installationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ClientHello.InstallationId, installationId);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.ClientHello.ClientTimeMs, value.ClientTimeMs);
        if (value.ResumeToken is { } resumeToken)
        {
            writer.WriteString(CoreCommandFieldNumbers.ClientHello.ResumeToken, resumeToken);
        }

        if (value.MaxPayloadBytes is { } maxPayloadBytes)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.ClientHello.MaxPayloadBytes, maxPayloadBytes);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ServerHello))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.ProtocolVersion, nameof(ServerHello.ProtocolVersion))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.FeatureBits, nameof(ServerHello.FeatureBits))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.ServerDeviceId, nameof(ServerHello.ServerDeviceId))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.ServerTimeMs, nameof(ServerHello.ServerTimeMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.HeartbeatIntervalMs, nameof(ServerHello.HeartbeatIntervalMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.MaxPayloadBytes, nameof(ServerHello.MaxPayloadBytes))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.ResumeSupported, nameof(ServerHello.ResumeSupported))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.PayloadFormat, nameof(ServerHello.PayloadFormat))]
public static partial class ServerHelloSchema
{
    public static BinaryStatus TryEncode(
        in ServerHello value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ServerHelloSchemaEncoder, ServerHello>(in value, destination, limits, out written);
}

public readonly struct ServerHelloSchemaEncoder : IBinaryEncoder<ServerHelloSchemaEncoder, ServerHello>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ServerHello value)
    {
        writer.WriteUInt32(CoreCommandFieldNumbers.ServerHello.ProtocolVersion, value.ProtocolVersion);
        writer.WriteUInt32(CoreCommandFieldNumbers.ServerHello.FeatureBits, value.FeatureBits);
        writer.WriteString(CoreCommandFieldNumbers.ServerHello.ServerDeviceId, value.ServerDeviceId);
        writer.WriteInt64(CoreCommandFieldNumbers.ServerHello.ServerTimeMs, value.ServerTimeMs);
        writer.WriteInt32(CoreCommandFieldNumbers.ServerHello.HeartbeatIntervalMs, value.HeartbeatIntervalMs);
        writer.WriteInt32(CoreCommandFieldNumbers.ServerHello.MaxPayloadBytes, value.MaxPayloadBytes);
        writer.WriteBool(CoreCommandFieldNumbers.ServerHello.ResumeSupported, value.ResumeSupported);
        writer.WriteString(CoreCommandFieldNumbers.ServerHello.PayloadFormat, value.PayloadFormat);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(GoAway))]
[TcpBinaryField(CoreCommandFieldNumbers.GoAway.RetryAfterMs, nameof(GoAway.RetryAfterMs))]
[TcpBinaryField(CoreCommandFieldNumbers.GoAway.Reason, nameof(GoAway.Reason))]
[TcpBinaryField(CoreCommandFieldNumbers.GoAway.ServerHint, nameof(GoAway.ServerHint))]
public static partial class GoAwaySchema
{
    public static BinaryStatus TryEncode(
        in GoAway value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<GoAwaySchemaEncoder, GoAway>(in value, destination, limits, out written);
}

public readonly struct GoAwaySchemaEncoder : IBinaryEncoder<GoAwaySchemaEncoder, GoAway>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in GoAway value)
    {
        writer.WriteInt32(CoreCommandFieldNumbers.GoAway.RetryAfterMs, value.RetryAfterMs);
        if (value.Reason is { } reason)
        {
            writer.WriteString(CoreCommandFieldNumbers.GoAway.Reason, reason);
        }

        if (value.ServerHint is { } serverHint)
        {
            writer.WriteString(CoreCommandFieldNumbers.GoAway.ServerHint, serverHint);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ResumeResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.Success, nameof(ResumeResponse.Success))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.FailureKind, nameof(ResumeResponse.FailureKind))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.ResumeToken, nameof(ResumeResponse.ResumeToken))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.UserId, nameof(ResumeResponse.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.SessionId, nameof(ResumeResponse.SessionId))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.DeviceId, nameof(ResumeResponse.DeviceId))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.LastConversationSequence, nameof(ResumeResponse.LastConversationSequence))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.ErrorMessage, nameof(ResumeResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.RetryAfterMs, nameof(ResumeResponse.RetryAfterMs))]
public static partial class ResumeResponseSchema
{
    public static BinaryStatus TryEncode(
        in ResumeResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ResumeResponseSchemaEncoder, ResumeResponse>(in value, destination, limits, out written);
}

public readonly struct ResumeResponseSchemaEncoder : IBinaryEncoder<ResumeResponseSchemaEncoder, ResumeResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ResumeResponse value)
    {
        writer.WriteBool(CoreCommandFieldNumbers.ResumeResponse.Success, value.Success);
        writer.WriteUInt32(CoreCommandFieldNumbers.ResumeResponse.FailureKind, (byte)value.FailureKind);
        if (value.ResumeToken is { } resumeToken)
        {
            writer.WriteString(CoreCommandFieldNumbers.ResumeResponse.ResumeToken, resumeToken);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.ResumeResponse.UserId, value.UserId);
        if (value.SessionId is { } sessionId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ResumeResponse.SessionId, sessionId);
        }

        if (value.DeviceId is { } deviceId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ResumeResponse.DeviceId, deviceId);
        }

        if (value.LastConversationSequence is { } lastConversationSequence)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ResumeResponse.LastConversationSequence, lastConversationSequence);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.ResumeResponse.ErrorMessage, errorMessage);
        }

        if (value.RetryAfterMs is { } retryAfterMs)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.ResumeResponse.RetryAfterMs, retryAfterMs);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ProtocolErrorFrame))]
[TcpBinaryField(CoreCommandFieldNumbers.ProtocolErrorFrame.Code, nameof(ProtocolErrorFrame.Code))]
[TcpBinaryField(CoreCommandFieldNumbers.ProtocolErrorFrame.Fatal, nameof(ProtocolErrorFrame.Fatal))]
[TcpBinaryField(CoreCommandFieldNumbers.ProtocolErrorFrame.RetryAfterMs, nameof(ProtocolErrorFrame.RetryAfterMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ProtocolErrorFrame.Message, nameof(ProtocolErrorFrame.Message))]
[TcpBinaryField(CoreCommandFieldNumbers.ProtocolErrorFrame.OriginCommand, nameof(ProtocolErrorFrame.OriginCommand))]
public static partial class ProtocolErrorFrameSchema
{
    public static BinaryStatus TryEncode(
        in ProtocolErrorFrame value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ProtocolErrorFrameSchemaEncoder, ProtocolErrorFrame>(in value, destination, limits, out written);
}

public readonly struct ProtocolErrorFrameSchemaEncoder : IBinaryEncoder<ProtocolErrorFrameSchemaEncoder, ProtocolErrorFrame>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ProtocolErrorFrame value)
    {
        writer.WriteUInt32(CoreCommandFieldNumbers.ProtocolErrorFrame.Code, (ushort)value.Code);
        writer.WriteBool(CoreCommandFieldNumbers.ProtocolErrorFrame.Fatal, value.Fatal);
        if (value.RetryAfterMs is { } retryAfterMs)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.ProtocolErrorFrame.RetryAfterMs, retryAfterMs);
        }

        if (value.Message is { } message)
        {
            writer.WriteString(CoreCommandFieldNumbers.ProtocolErrorFrame.Message, message);
        }

        if (value.OriginCommand is { } originCommand)
        {
            writer.WriteUInt32(CoreCommandFieldNumbers.ProtocolErrorFrame.OriginCommand, originCommand);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageHistoryRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.RequestId, nameof(MessageHistoryRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.ConversationId, nameof(MessageHistoryRequest.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.BeforeReceivedAtMs, nameof(MessageHistoryRequest.BeforeReceivedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.BeforeMessageId, nameof(MessageHistoryRequest.BeforeMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.AfterReceivedAtMs, nameof(MessageHistoryRequest.AfterReceivedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.AfterMessageId, nameof(MessageHistoryRequest.AfterMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.Limit, nameof(MessageHistoryRequest.Limit))]
public static partial class MessageHistoryRequestSchema
{
    public static BinaryStatus TryEncode(
        in MessageHistoryRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryRequestSchemaEncoder, MessageHistoryRequest>(in value, destination, limits, out written);
}

public readonly struct MessageHistoryRequestSchemaEncoder : IBinaryEncoder<MessageHistoryRequestSchemaEncoder, MessageHistoryRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryRequest.RequestId, requestId);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryRequest.ConversationId, conversationId);
        }

        if (value.BeforeReceivedAtMs is { } beforeReceivedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryRequest.BeforeReceivedAtMs, beforeReceivedAtMs);
        }

        if (value.BeforeMessageId is { } beforeMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryRequest.BeforeMessageId, beforeMessageId);
        }

        if (value.AfterReceivedAtMs is { } afterReceivedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryRequest.AfterReceivedAtMs, afterReceivedAtMs);
        }

        if (value.AfterMessageId is { } afterMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryRequest.AfterMessageId, afterMessageId);
        }

        writer.WriteInt32(CoreCommandFieldNumbers.MessageHistoryRequest.Limit, value.Limit);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageHistoryCursor))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryCursor.ReceivedAtMs, nameof(MessageHistoryCursor.ReceivedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryCursor.ChangedAtMs, nameof(MessageHistoryCursor.ChangedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryCursor.MessageId, nameof(MessageHistoryCursor.MessageId))]
public static partial class MessageHistoryCursorSchema
{
    public static BinaryStatus TryEncode(
        in MessageHistoryCursor value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryCursorSchemaEncoder, MessageHistoryCursor>(in value, destination, limits, out written);
}

public readonly struct MessageHistoryCursorSchemaEncoder : IBinaryEncoder<MessageHistoryCursorSchemaEncoder, MessageHistoryCursor>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryCursor value)
    {
        writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryCursor.ReceivedAtMs, value.ReceivedAtMs);
        if (value.ChangedAtMs is { } changedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryCursor.ChangedAtMs, changedAtMs);
        }

        writer.WriteString(CoreCommandFieldNumbers.MessageHistoryCursor.MessageId, value.MessageId);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpAttachmentRef))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.RefVersion, nameof(TcpAttachmentRef.RefVersion))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.AttachmentId, nameof(TcpAttachmentRef.AttachmentId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.FileName, nameof(TcpAttachmentRef.FileName))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.ContentType, nameof(TcpAttachmentRef.ContentType))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.SizeBytes, nameof(TcpAttachmentRef.SizeBytes))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.Status, nameof(TcpAttachmentRef.Status))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.DownloadApiHint, nameof(TcpAttachmentRef.DownloadApiHint))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.DownloadToken, nameof(TcpAttachmentRef.DownloadToken))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.ThumbnailApiHint, nameof(TcpAttachmentRef.ThumbnailApiHint))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.IsVoice, nameof(TcpAttachmentRef.IsVoice))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceCodec, nameof(TcpAttachmentRef.VoiceCodec))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceContainer, nameof(TcpAttachmentRef.VoiceContainer))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceDurationMs, nameof(TcpAttachmentRef.VoiceDurationMs))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceSampleRateHz, nameof(TcpAttachmentRef.VoiceSampleRateHz))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceChannels, nameof(TcpAttachmentRef.VoiceChannels))]
public static partial class TcpAttachmentRefSchema
{
    public static BinaryStatus TryEncode(
        in TcpAttachmentRef value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpAttachmentRefSchemaEncoder, TcpAttachmentRef>(in value, destination, limits, out written);
}

public readonly struct TcpAttachmentRefSchemaEncoder : IBinaryEncoder<TcpAttachmentRefSchemaEncoder, TcpAttachmentRef>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpAttachmentRef value)
    {
        writer.WriteInt32(CoreCommandFieldNumbers.TcpAttachmentRef.RefVersion, value.RefVersion);
        writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.AttachmentId, value.AttachmentId);
        if (value.FileName is { } fileName)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.FileName, fileName);
        }

        writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.ContentType, value.ContentType);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpAttachmentRef.SizeBytes, value.SizeBytes);
        writer.WriteInt32(CoreCommandFieldNumbers.TcpAttachmentRef.Status, value.Status);
        if (value.DownloadApiHint is { } downloadApiHint)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.DownloadApiHint, downloadApiHint);
        }

        if (value.DownloadToken is { } downloadToken)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.DownloadToken, downloadToken);
        }

        if (value.ThumbnailApiHint is { } thumbnailApiHint)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.ThumbnailApiHint, thumbnailApiHint);
        }

        writer.WriteBool(CoreCommandFieldNumbers.TcpAttachmentRef.IsVoice, value.IsVoice);
        if (value.VoiceCodec is { } voiceCodec)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceCodec, voiceCodec);
        }

        if (value.VoiceContainer is { } voiceContainer)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceContainer, voiceContainer);
        }

        if (value.VoiceDurationMs is { } voiceDurationMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceDurationMs, voiceDurationMs);
        }

        if (value.VoiceSampleRateHz is { } voiceSampleRateHz)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceSampleRateHz, voiceSampleRateHz);
        }

        if (value.VoiceChannels is { } voiceChannels)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceChannels, voiceChannels);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageReactionSummary))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReactionSummary.Emoji, nameof(MessageReactionSummary.Emoji))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReactionSummary.Count, nameof(MessageReactionSummary.Count))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReactionSummary.ReactedByMe, nameof(MessageReactionSummary.ReactedByMe))]
public static partial class MessageReactionSummarySchema
{
    public static BinaryStatus TryEncode(
        in MessageReactionSummary value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageReactionSummarySchemaEncoder, MessageReactionSummary>(in value, destination, limits, out written);
}

public readonly struct MessageReactionSummarySchemaEncoder : IBinaryEncoder<MessageReactionSummarySchemaEncoder, MessageReactionSummary>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageReactionSummary value)
    {
        writer.WriteString(CoreCommandFieldNumbers.MessageReactionSummary.Emoji, value.Emoji);
        writer.WriteInt32(CoreCommandFieldNumbers.MessageReactionSummary.Count, value.Count);
        writer.WriteBool(CoreCommandFieldNumbers.MessageReactionSummary.ReactedByMe, value.ReactedByMe);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageHistoryItem))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.MessageId, nameof(MessageHistoryItem.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ClientMessageId, nameof(MessageHistoryItem.ClientMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.SenderUserId, nameof(MessageHistoryItem.SenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ReceiverUserId, nameof(MessageHistoryItem.ReceiverUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ConversationId, nameof(MessageHistoryItem.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.Content, nameof(MessageHistoryItem.Content))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ReceivedAtMs, nameof(MessageHistoryItem.ReceivedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.DeliveredAtMs, nameof(MessageHistoryItem.DeliveredAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ReadAtMs, nameof(MessageHistoryItem.ReadAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.RecalledAtMs, nameof(MessageHistoryItem.RecalledAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.EditVersion, nameof(MessageHistoryItem.EditVersion))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.EditedAtMs, nameof(MessageHistoryItem.EditedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ChangedAtMs, nameof(MessageHistoryItem.ChangedAtMs))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.MessageHistoryItem.Attachments, nameof(MessageHistoryItem.Attachments), typeof(TcpAttachmentRefSchema))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.MessageHistoryItem.Reactions, nameof(MessageHistoryItem.Reactions), typeof(MessageReactionSummarySchema))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ReplyToMessageId, nameof(MessageHistoryItem.ReplyToMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ReplyToSenderUserId, nameof(MessageHistoryItem.ReplyToSenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ReplyToPreview, nameof(MessageHistoryItem.ReplyToPreview))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ForwardedFromMessageId, nameof(MessageHistoryItem.ForwardedFromMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ForwardedFromSenderUserId, nameof(MessageHistoryItem.ForwardedFromSenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ForwardedFromPreview, nameof(MessageHistoryItem.ForwardedFromPreview))]
[TcpBinaryRepeatedField(CoreCommandFieldNumbers.MessageHistoryItem.MentionedUserIds, nameof(MessageHistoryItem.MentionedUserIds))]
[TcpBinaryRepeatedField(CoreCommandFieldNumbers.MessageHistoryItem.MentionedRoles, nameof(MessageHistoryItem.MentionedRoles))]
public static partial class MessageHistoryItemSchema
{
    public static BinaryStatus TryEncode(
        in MessageHistoryItem value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryItemSchemaEncoder, MessageHistoryItem>(in value, destination, limits, out written);
}

public readonly struct MessageHistoryItemSchemaEncoder : IBinaryEncoder<MessageHistoryItemSchemaEncoder, MessageHistoryItem>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryItem value)
    {
        writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.MessageId, value.MessageId);
        writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.ClientMessageId, value.ClientMessageId);
        writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.SenderUserId, value.SenderUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.ReceiverUserId, value.ReceiverUserId);
        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.ConversationId, conversationId);
        }

        writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.Content, value.Content);
        writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.ReceivedAtMs, value.ReceivedAtMs);
        if (value.DeliveredAtMs is { } deliveredAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.DeliveredAtMs, deliveredAtMs);
        }

        if (value.ReadAtMs is { } readAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.ReadAtMs, readAtMs);
        }

        if (value.RecalledAtMs is { } recalledAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.RecalledAtMs, recalledAtMs);
        }

        writer.WriteInt32(CoreCommandFieldNumbers.MessageHistoryItem.EditVersion, value.EditVersion);
        if (value.EditedAtMs is { } editedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.EditedAtMs, editedAtMs);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.ChangedAtMs, value.ChangedAtMs);

        if (value.Attachments is { } attachments)
        {
            foreach (TcpAttachmentRef attachment in attachments)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.MessageHistoryItem.Attachments)) return writer.Status;
                writer.WriteNested<TcpAttachmentRefSchemaEncoder, TcpAttachmentRef>(CoreCommandFieldNumbers.MessageHistoryItem.Attachments, in attachment, allowRepeatedFieldNumber: true);
            }
        }

        if (value.Reactions is { } reactions)
        {
            foreach (MessageReactionSummary reaction in reactions)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.MessageHistoryItem.Reactions)) return writer.Status;
                writer.WriteNested<MessageReactionSummarySchemaEncoder, MessageReactionSummary>(CoreCommandFieldNumbers.MessageHistoryItem.Reactions, in reaction, allowRepeatedFieldNumber: true);
            }
        }

        if (value.ReplyToMessageId is { } replyToMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.ReplyToMessageId, replyToMessageId);
        }

        if (value.ReplyToSenderUserId is { } replyToSenderUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.ReplyToSenderUserId, replyToSenderUserId);
        }

        if (value.ReplyToPreview is { } replyToPreview)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.ReplyToPreview, replyToPreview);
        }

        if (value.ForwardedFromMessageId is { } forwardedFromMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.ForwardedFromMessageId, forwardedFromMessageId);
        }

        if (value.ForwardedFromSenderUserId is { } forwardedFromSenderUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.ForwardedFromSenderUserId, forwardedFromSenderUserId);
        }

        if (value.ForwardedFromPreview is { } forwardedFromPreview)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.ForwardedFromPreview, forwardedFromPreview);
        }

        if (value.MentionedUserIds is { } mentionedUserIds)
        {
            foreach (long mentionedUserId in mentionedUserIds)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.MessageHistoryItem.MentionedUserIds)) return writer.Status;
                writer.WriteRepeatedInt64(CoreCommandFieldNumbers.MessageHistoryItem.MentionedUserIds, mentionedUserId);
            }
        }

        if (value.MentionedRoles is { } mentionedRoles)
        {
            foreach (string mentionedRole in mentionedRoles)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.MessageHistoryItem.MentionedRoles)) return writer.Status;
                writer.WriteRepeatedString(CoreCommandFieldNumbers.MessageHistoryItem.MentionedRoles, mentionedRole);
            }
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageHistoryResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryResponse.RequestId, nameof(MessageHistoryResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryResponse.ConversationId, nameof(MessageHistoryResponse.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryResponse.Succeeded, nameof(MessageHistoryResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryResponse.ErrorCode, nameof(MessageHistoryResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryResponse.ErrorMessage, nameof(MessageHistoryResponse.ErrorMessage))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.MessageHistoryResponse.Items, nameof(MessageHistoryResponse.Items), typeof(MessageHistoryItemSchema))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.MessageHistoryResponse.NextCursor, nameof(MessageHistoryResponse.NextCursor), typeof(MessageHistoryCursorSchema))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryResponse.HasMore, nameof(MessageHistoryResponse.HasMore))]
public static partial class MessageHistoryResponseSchema
{
    public static BinaryStatus TryEncode(
        in MessageHistoryResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryResponseSchemaEncoder, MessageHistoryResponse>(in value, destination, limits, out written);
}

public readonly struct MessageHistoryResponseSchemaEncoder : IBinaryEncoder<MessageHistoryResponseSchemaEncoder, MessageHistoryResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryResponse value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryResponse.RequestId, requestId);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryResponse.ConversationId, conversationId);
        }

        writer.WriteBool(CoreCommandFieldNumbers.MessageHistoryResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryResponse.ErrorMessage, errorMessage);
        }

        if (value.Items is { } items)
        {
            foreach (MessageHistoryItem item in items)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.MessageHistoryResponse.Items)) return writer.Status;
                writer.WriteNested<MessageHistoryItemSchemaEncoder, MessageHistoryItem>(CoreCommandFieldNumbers.MessageHistoryResponse.Items, in item, allowRepeatedFieldNumber: true);
            }
        }

        if (value.NextCursor is { } nextCursor)
        {
            writer.WriteNested<MessageHistoryCursorSchemaEncoder, MessageHistoryCursor>(CoreCommandFieldNumbers.MessageHistoryResponse.NextCursor, in nextCursor);
        }

        writer.WriteBool(CoreCommandFieldNumbers.MessageHistoryResponse.HasMore, value.HasMore);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(AuthenticationRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.AuthenticationRequest.AccessToken, nameof(AuthenticationRequest.AccessToken))]
[TcpBinaryField(CoreCommandFieldNumbers.AuthenticationRequest.DeviceIdHash, nameof(AuthenticationRequest.DeviceIdHash))]
public static partial class AuthenticationRequestSchema
{
    public static BinaryStatus TryEncode(
        in AuthenticationRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<AuthenticationRequestSchemaEncoder, AuthenticationRequest>(in value, destination, limits, out written);
}

public readonly struct AuthenticationRequestSchemaEncoder : IBinaryEncoder<AuthenticationRequestSchemaEncoder, AuthenticationRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in AuthenticationRequest value)
    {
        writer.WriteString(CoreCommandFieldNumbers.AuthenticationRequest.AccessToken, value.AccessToken);
        if (value.DeviceIdHash is { } deviceIdHash)
        {
            writer.WriteUInt64(CoreCommandFieldNumbers.AuthenticationRequest.DeviceIdHash, deviceIdHash);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(AuthenticationResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.AuthenticationResponse.Success, nameof(AuthenticationResponse.Success))]
[TcpBinaryField(CoreCommandFieldNumbers.AuthenticationResponse.UserId, nameof(AuthenticationResponse.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.AuthenticationResponse.ErrorMessage, nameof(AuthenticationResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.AuthenticationResponse.SessionId, nameof(AuthenticationResponse.SessionId))]
[TcpBinaryField(CoreCommandFieldNumbers.AuthenticationResponse.DeviceIdHash, nameof(AuthenticationResponse.DeviceIdHash))]
[TcpBinaryField(CoreCommandFieldNumbers.AuthenticationResponse.DeviceId, nameof(AuthenticationResponse.DeviceId))]
[TcpBinaryField(CoreCommandFieldNumbers.AuthenticationResponse.ResumeToken, nameof(AuthenticationResponse.ResumeToken))]
public static partial class AuthenticationResponseSchema
{
    public static BinaryStatus TryEncode(
        in AuthenticationResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<AuthenticationResponseSchemaEncoder, AuthenticationResponse>(in value, destination, limits, out written);
}

public readonly struct AuthenticationResponseSchemaEncoder : IBinaryEncoder<AuthenticationResponseSchemaEncoder, AuthenticationResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in AuthenticationResponse value)
    {
        writer.WriteBool(CoreCommandFieldNumbers.AuthenticationResponse.Success, value.Success);
        writer.WriteInt64(CoreCommandFieldNumbers.AuthenticationResponse.UserId, value.UserId);
        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.AuthenticationResponse.ErrorMessage, errorMessage);
        }

        if (value.SessionId is { } sessionId)
        {
            writer.WriteString(CoreCommandFieldNumbers.AuthenticationResponse.SessionId, sessionId);
        }

        if (value.DeviceIdHash is { } deviceIdHash)
        {
            writer.WriteUInt64(CoreCommandFieldNumbers.AuthenticationResponse.DeviceIdHash, deviceIdHash);
        }

        if (value.DeviceId is { } deviceId)
        {
            writer.WriteString(CoreCommandFieldNumbers.AuthenticationResponse.DeviceId, deviceId);
        }

        if (value.ResumeToken is { } resumeToken)
        {
            writer.WriteString(CoreCommandFieldNumbers.AuthenticationResponse.ResumeToken, resumeToken);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ChatMessage))]
[TcpBinaryRepeatedField(CoreCommandFieldNumbers.ChatMessage.AttachmentIds, nameof(ChatMessage.AttachmentIds))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.ChatMessage.Attachments, nameof(ChatMessage.Attachments), typeof(TcpAttachmentRefSchema))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.ClientMessageId, nameof(ChatMessage.ClientMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.MessageId, nameof(ChatMessage.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.ConversationId, nameof(ChatMessage.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.TargetUserId, nameof(ChatMessage.TargetUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.SenderUserId, nameof(ChatMessage.SenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.Content, nameof(ChatMessage.Content))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.SentAtMs, nameof(ChatMessage.SentAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.ReplyToMessageId, nameof(ChatMessage.ReplyToMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.ReplyToSenderUserId, nameof(ChatMessage.ReplyToSenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.ReplyToPreview, nameof(ChatMessage.ReplyToPreview))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.ForwardedFromMessageId, nameof(ChatMessage.ForwardedFromMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.ForwardedFromSenderUserId, nameof(ChatMessage.ForwardedFromSenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ChatMessage.ForwardedFromPreview, nameof(ChatMessage.ForwardedFromPreview))]
[TcpBinaryRepeatedField(CoreCommandFieldNumbers.ChatMessage.MentionedUserIds, nameof(ChatMessage.MentionedUserIds))]
[TcpBinaryRepeatedField(CoreCommandFieldNumbers.ChatMessage.MentionedRoles, nameof(ChatMessage.MentionedRoles))]
public static partial class ChatMessageSchema
{
    public static BinaryStatus TryEncode(
        in ChatMessage value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ChatMessageSchemaEncoder, ChatMessage>(in value, destination, limits, out written);
}

public readonly struct ChatMessageSchemaEncoder : IBinaryEncoder<ChatMessageSchemaEncoder, ChatMessage>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ChatMessage value)
    {
        if (value.ClientMessageId is { } clientMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ChatMessage.ClientMessageId, clientMessageId);
        }

        writer.WriteString(CoreCommandFieldNumbers.ChatMessage.MessageId, value.MessageId);
        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ChatMessage.ConversationId, conversationId);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.ChatMessage.TargetUserId, value.TargetUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.ChatMessage.SenderUserId, value.SenderUserId);
        writer.WriteString(CoreCommandFieldNumbers.ChatMessage.Content, value.Content);
        writer.WriteInt64(CoreCommandFieldNumbers.ChatMessage.SentAtMs, value.SentAtMs);

        if (value.AttachmentIds is { } attachmentIds)
        {
            foreach (string attachmentId in attachmentIds)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.ChatMessage.AttachmentIds)) return writer.Status;
                writer.WriteRepeatedString(CoreCommandFieldNumbers.ChatMessage.AttachmentIds, attachmentId);
            }
        }

        if (value.Attachments is { } attachments)
        {
            foreach (TcpAttachmentRef attachment in attachments)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.ChatMessage.Attachments)) return writer.Status;
                writer.WriteNested<TcpAttachmentRefSchemaEncoder, TcpAttachmentRef>(CoreCommandFieldNumbers.ChatMessage.Attachments, in attachment, allowRepeatedFieldNumber: true);
            }
        }

        if (value.ReplyToMessageId is { } replyToMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ChatMessage.ReplyToMessageId, replyToMessageId);
        }

        if (value.ReplyToSenderUserId is { } replyToSenderUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ChatMessage.ReplyToSenderUserId, replyToSenderUserId);
        }

        if (value.ReplyToPreview is { } replyToPreview)
        {
            writer.WriteString(CoreCommandFieldNumbers.ChatMessage.ReplyToPreview, replyToPreview);
        }

        if (value.ForwardedFromMessageId is { } forwardedFromMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ChatMessage.ForwardedFromMessageId, forwardedFromMessageId);
        }

        if (value.ForwardedFromSenderUserId is { } forwardedFromSenderUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ChatMessage.ForwardedFromSenderUserId, forwardedFromSenderUserId);
        }

        if (value.ForwardedFromPreview is { } forwardedFromPreview)
        {
            writer.WriteString(CoreCommandFieldNumbers.ChatMessage.ForwardedFromPreview, forwardedFromPreview);
        }

        if (value.MentionedUserIds is { } mentionedUserIds)
        {
            foreach (long mentionedUserId in mentionedUserIds)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.ChatMessage.MentionedUserIds)) return writer.Status;
                writer.WriteRepeatedInt64(CoreCommandFieldNumbers.ChatMessage.MentionedUserIds, mentionedUserId);
            }
        }

        if (value.MentionedRoles is { } mentionedRoles)
        {
            foreach (string mentionedRole in mentionedRoles)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.ChatMessage.MentionedRoles)) return writer.Status;
                writer.WriteRepeatedString(CoreCommandFieldNumbers.ChatMessage.MentionedRoles, mentionedRole);
            }
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageAcknowledgement))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageAcknowledgement.ClientMessageId, nameof(MessageAcknowledgement.ClientMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageAcknowledgement.CommandId, nameof(MessageAcknowledgement.CommandId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageAcknowledgement.Accepted, nameof(MessageAcknowledgement.Accepted))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageAcknowledgement.ErrorCode, nameof(MessageAcknowledgement.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageAcknowledgement.ErrorMessage, nameof(MessageAcknowledgement.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageAcknowledgement.AcknowledgedAtMs, nameof(MessageAcknowledgement.AcknowledgedAtMs))]
public static partial class MessageAcknowledgementSchema
{
    public static BinaryStatus TryEncode(
        in MessageAcknowledgement value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageAcknowledgementSchemaEncoder, MessageAcknowledgement>(in value, destination, limits, out written);
}

public readonly struct MessageAcknowledgementSchemaEncoder : IBinaryEncoder<MessageAcknowledgementSchemaEncoder, MessageAcknowledgement>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageAcknowledgement value)
    {
        if (value.ClientMessageId is { } clientMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageAcknowledgement.ClientMessageId, clientMessageId);
        }

        if (value.CommandId is { } commandId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageAcknowledgement.CommandId, commandId);
        }

        writer.WriteBool(CoreCommandFieldNumbers.MessageAcknowledgement.Accepted, value.Accepted);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageAcknowledgement.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageAcknowledgement.ErrorMessage, errorMessage);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.MessageAcknowledgement.AcknowledgedAtMs, value.AcknowledgedAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageReceipt))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceipt.RequestId, nameof(MessageReceipt.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceipt.ConversationId, nameof(MessageReceipt.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceipt.LastReadMessageId, nameof(MessageReceipt.LastReadMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceipt.LastReadAtMs, nameof(MessageReceipt.LastReadAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceipt.ReaderUserId, nameof(MessageReceipt.ReaderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceipt.ReceiverUserId, nameof(MessageReceipt.ReceiverUserId))]
public static partial class MessageReceiptSchema
{
    public static BinaryStatus TryEncode(
        in MessageReceipt value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageReceiptSchemaEncoder, MessageReceipt>(in value, destination, limits, out written);
}

public readonly struct MessageReceiptSchemaEncoder : IBinaryEncoder<MessageReceiptSchemaEncoder, MessageReceipt>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageReceipt value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageReceipt.RequestId, requestId);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageReceipt.ConversationId, conversationId);
        }

        if (value.LastReadMessageId is { } lastReadMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageReceipt.LastReadMessageId, lastReadMessageId);
        }

        if (value.LastReadAtMs is { } lastReadAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageReceipt.LastReadAtMs, lastReadAtMs);
        }

        if (value.ReaderUserId is { } readerUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageReceipt.ReaderUserId, readerUserId);
        }

        if (value.ReceiverUserId is { } receiverUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageReceipt.ReceiverUserId, receiverUserId);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageReceiptAcknowledgement))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceiptAcknowledgement.RequestId, nameof(MessageReceiptAcknowledgement.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceiptAcknowledgement.Accepted, nameof(MessageReceiptAcknowledgement.Accepted))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceiptAcknowledgement.ErrorCode, nameof(MessageReceiptAcknowledgement.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceiptAcknowledgement.ErrorMessage, nameof(MessageReceiptAcknowledgement.ErrorMessage))]
public static partial class MessageReceiptAcknowledgementSchema
{
    public static BinaryStatus TryEncode(
        in MessageReceiptAcknowledgement value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageReceiptAcknowledgementSchemaEncoder, MessageReceiptAcknowledgement>(in value, destination, limits, out written);
}

public readonly struct MessageReceiptAcknowledgementSchemaEncoder : IBinaryEncoder<MessageReceiptAcknowledgementSchemaEncoder, MessageReceiptAcknowledgement>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageReceiptAcknowledgement value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageReceiptAcknowledgement.RequestId, requestId);
        }

        writer.WriteBool(CoreCommandFieldNumbers.MessageReceiptAcknowledgement.Accepted, value.Accepted);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageReceiptAcknowledgement.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageReceiptAcknowledgement.ErrorMessage, errorMessage);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageReceiptUpdated))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceiptUpdated.ConversationId, nameof(MessageReceiptUpdated.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceiptUpdated.LastReadMessageId, nameof(MessageReceiptUpdated.LastReadMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceiptUpdated.LastReadAtMs, nameof(MessageReceiptUpdated.LastReadAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReceiptUpdated.ReaderUserId, nameof(MessageReceiptUpdated.ReaderUserId))]
public static partial class MessageReceiptUpdatedSchema
{
    public static BinaryStatus TryEncode(
        in MessageReceiptUpdated value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageReceiptUpdatedSchemaEncoder, MessageReceiptUpdated>(in value, destination, limits, out written);
}

public readonly struct MessageReceiptUpdatedSchemaEncoder : IBinaryEncoder<MessageReceiptUpdatedSchemaEncoder, MessageReceiptUpdated>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageReceiptUpdated value)
    {
        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageReceiptUpdated.ConversationId, conversationId);
        }

        if (value.LastReadMessageId is { } lastReadMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageReceiptUpdated.LastReadMessageId, lastReadMessageId);
        }

        if (value.LastReadAtMs is { } lastReadAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageReceiptUpdated.LastReadAtMs, lastReadAtMs);
        }

        if (value.ReaderUserId is { } readerUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageReceiptUpdated.ReaderUserId, readerUserId);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(AttachmentLifecycleChanged))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentLifecycleChanged.AttachmentId, nameof(AttachmentLifecycleChanged.AttachmentId))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentLifecycleChanged.Status, nameof(AttachmentLifecycleChanged.Status))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentLifecycleChanged.OccurredAtMs, nameof(AttachmentLifecycleChanged.OccurredAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentLifecycleChanged.RejectReason, nameof(AttachmentLifecycleChanged.RejectReason))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentLifecycleChanged.ThumbnailApiHint, nameof(AttachmentLifecycleChanged.ThumbnailApiHint))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentLifecycleChanged.DownloadToken, nameof(AttachmentLifecycleChanged.DownloadToken))]
public static partial class AttachmentLifecycleChangedSchema
{
    public static BinaryStatus TryEncode(
        in AttachmentLifecycleChanged value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<AttachmentLifecycleChangedSchemaEncoder, AttachmentLifecycleChanged>(in value, destination, limits, out written);
}

public readonly struct AttachmentLifecycleChangedSchemaEncoder : IBinaryEncoder<AttachmentLifecycleChangedSchemaEncoder, AttachmentLifecycleChanged>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in AttachmentLifecycleChanged value)
    {
        writer.WriteString(CoreCommandFieldNumbers.AttachmentLifecycleChanged.AttachmentId, value.AttachmentId);
        writer.WriteInt32(CoreCommandFieldNumbers.AttachmentLifecycleChanged.Status, value.Status);
        writer.WriteInt64(CoreCommandFieldNumbers.AttachmentLifecycleChanged.OccurredAtMs, value.OccurredAtMs);
        if (value.RejectReason is { } rejectReason)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentLifecycleChanged.RejectReason, rejectReason);
        }

        if (value.ThumbnailApiHint is { } thumbnailApiHint)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentLifecycleChanged.ThumbnailApiHint, thumbnailApiHint);
        }

        if (value.DownloadToken is { } downloadToken)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentLifecycleChanged.DownloadToken, downloadToken);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(AttachmentFinalizeRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentFinalizeRequest.RequestId, nameof(AttachmentFinalizeRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentFinalizeRequest.AttachmentId, nameof(AttachmentFinalizeRequest.AttachmentId))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentFinalizeRequest.SizeBytes, nameof(AttachmentFinalizeRequest.SizeBytes))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentFinalizeRequest.ContentHash, nameof(AttachmentFinalizeRequest.ContentHash))]
public static partial class AttachmentFinalizeRequestSchema
{
    public static BinaryStatus TryEncode(
        in AttachmentFinalizeRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<AttachmentFinalizeRequestSchemaEncoder, AttachmentFinalizeRequest>(in value, destination, limits, out written);
}

public readonly struct AttachmentFinalizeRequestSchemaEncoder : IBinaryEncoder<AttachmentFinalizeRequestSchemaEncoder, AttachmentFinalizeRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in AttachmentFinalizeRequest value)
    {
        writer.WriteString(CoreCommandFieldNumbers.AttachmentFinalizeRequest.RequestId, value.RequestId);
        writer.WriteString(CoreCommandFieldNumbers.AttachmentFinalizeRequest.AttachmentId, value.AttachmentId);
        writer.WriteInt64(CoreCommandFieldNumbers.AttachmentFinalizeRequest.SizeBytes, value.SizeBytes);
        if (value.ContentHash is { } contentHash)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentFinalizeRequest.ContentHash, contentHash);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(AttachmentFinalizeResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentFinalizeResponse.RequestId, nameof(AttachmentFinalizeResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentFinalizeResponse.Succeeded, nameof(AttachmentFinalizeResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentFinalizeResponse.ErrorCode, nameof(AttachmentFinalizeResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentFinalizeResponse.ErrorMessage, nameof(AttachmentFinalizeResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentFinalizeResponse.AttachmentId, nameof(AttachmentFinalizeResponse.AttachmentId))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentFinalizeResponse.Status, nameof(AttachmentFinalizeResponse.Status))]
public static partial class AttachmentFinalizeResponseSchema
{
    public static BinaryStatus TryEncode(
        in AttachmentFinalizeResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<AttachmentFinalizeResponseSchemaEncoder, AttachmentFinalizeResponse>(in value, destination, limits, out written);
}

public readonly struct AttachmentFinalizeResponseSchemaEncoder : IBinaryEncoder<AttachmentFinalizeResponseSchemaEncoder, AttachmentFinalizeResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in AttachmentFinalizeResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.AttachmentFinalizeResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.AttachmentFinalizeResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentFinalizeResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentFinalizeResponse.ErrorMessage, errorMessage);
        }

        if (value.AttachmentId is { } attachmentId)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentFinalizeResponse.AttachmentId, attachmentId);
        }

        if (value.Status is { } status)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.AttachmentFinalizeResponse.Status, status);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(AttachmentDownloadAuthorizeRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeRequest.RequestId, nameof(AttachmentDownloadAuthorizeRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeRequest.AttachmentId, nameof(AttachmentDownloadAuthorizeRequest.AttachmentId))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeRequest.ConversationId, nameof(AttachmentDownloadAuthorizeRequest.ConversationId))]
public static partial class AttachmentDownloadAuthorizeRequestSchema
{
    public static BinaryStatus TryEncode(
        in AttachmentDownloadAuthorizeRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<AttachmentDownloadAuthorizeRequestSchemaEncoder, AttachmentDownloadAuthorizeRequest>(in value, destination, limits, out written);
}

public readonly struct AttachmentDownloadAuthorizeRequestSchemaEncoder : IBinaryEncoder<AttachmentDownloadAuthorizeRequestSchemaEncoder, AttachmentDownloadAuthorizeRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in AttachmentDownloadAuthorizeRequest value)
    {
        writer.WriteString(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeRequest.RequestId, value.RequestId);
        writer.WriteString(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeRequest.AttachmentId, value.AttachmentId);
        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeRequest.ConversationId, conversationId);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(AttachmentDownloadAuthorizeResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.RequestId, nameof(AttachmentDownloadAuthorizeResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.Succeeded, nameof(AttachmentDownloadAuthorizeResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.ErrorCode, nameof(AttachmentDownloadAuthorizeResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.ErrorMessage, nameof(AttachmentDownloadAuthorizeResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.AttachmentId, nameof(AttachmentDownloadAuthorizeResponse.AttachmentId))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.DownloadUrl, nameof(AttachmentDownloadAuthorizeResponse.DownloadUrl))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.DownloadToken, nameof(AttachmentDownloadAuthorizeResponse.DownloadToken))]
[TcpBinaryField(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.ExpiresAtMs, nameof(AttachmentDownloadAuthorizeResponse.ExpiresAtMs))]
public static partial class AttachmentDownloadAuthorizeResponseSchema
{
    public static BinaryStatus TryEncode(
        in AttachmentDownloadAuthorizeResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<AttachmentDownloadAuthorizeResponseSchemaEncoder, AttachmentDownloadAuthorizeResponse>(in value, destination, limits, out written);
}

public readonly struct AttachmentDownloadAuthorizeResponseSchemaEncoder : IBinaryEncoder<AttachmentDownloadAuthorizeResponseSchemaEncoder, AttachmentDownloadAuthorizeResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in AttachmentDownloadAuthorizeResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.ErrorMessage, errorMessage);
        }

        if (value.AttachmentId is { } attachmentId)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.AttachmentId, attachmentId);
        }

        if (value.DownloadUrl is { } downloadUrl)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.DownloadUrl, downloadUrl);
        }

        if (value.DownloadToken is { } downloadToken)
        {
            writer.WriteString(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.DownloadToken, downloadToken);
        }

        if (value.ExpiresAtMs is { } expiresAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.AttachmentDownloadAuthorizeResponse.ExpiresAtMs, expiresAtMs);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpCallGrant))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallGrant.CallId, nameof(TcpCallGrant.CallId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallGrant.CallerUserId, nameof(TcpCallGrant.CallerUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallGrant.CalleeUserId, nameof(TcpCallGrant.CalleeUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallGrant.ExpiresAtMs, nameof(TcpCallGrant.ExpiresAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallGrant.Nonce, nameof(TcpCallGrant.Nonce))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallGrant.Signature, nameof(TcpCallGrant.Signature))]
public static partial class TcpCallGrantSchema
{
    public static BinaryStatus TryEncode(
        in TcpCallGrant value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpCallGrantSchemaEncoder, TcpCallGrant>(in value, destination, limits, out written);
}

public readonly struct TcpCallGrantSchemaEncoder : IBinaryEncoder<TcpCallGrantSchemaEncoder, TcpCallGrant>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpCallGrant value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpCallGrant.CallId, value.CallId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpCallGrant.CallerUserId, value.CallerUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpCallGrant.CalleeUserId, value.CalleeUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpCallGrant.ExpiresAtMs, value.ExpiresAtMs);
        writer.WriteString(CoreCommandFieldNumbers.TcpCallGrant.Nonce, value.Nonce);
        if (value.Signature is { } signature)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpCallGrant.Signature, signature);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpCallCommandRequest))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.TcpCallCommandRequest.Grant, nameof(TcpCallCommandRequest.Grant), typeof(TcpCallGrantSchema))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandRequest.RequestId, nameof(TcpCallCommandRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandRequest.CommandId, nameof(TcpCallCommandRequest.CommandId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandRequest.CallId, nameof(TcpCallCommandRequest.CallId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandRequest.Type, nameof(TcpCallCommandRequest.Type))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandRequest.ActorUserId, nameof(TcpCallCommandRequest.ActorUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandRequest.Revision, nameof(TcpCallCommandRequest.Revision))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandRequest.Sdp, nameof(TcpCallCommandRequest.Sdp))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandRequest.ClientOccurredAtMs, nameof(TcpCallCommandRequest.ClientOccurredAtMs))]
public static partial class TcpCallCommandRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpCallCommandRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpCallCommandRequestSchemaEncoder, TcpCallCommandRequest>(in value, destination, limits, out written);
}

public readonly struct TcpCallCommandRequestSchemaEncoder : IBinaryEncoder<TcpCallCommandRequestSchemaEncoder, TcpCallCommandRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpCallCommandRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpCallCommandRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.TcpCallCommandRequest.CommandId, value.CommandId);
        writer.WriteString(CoreCommandFieldNumbers.TcpCallCommandRequest.CallId, value.CallId);
        writer.WriteUInt32(CoreCommandFieldNumbers.TcpCallCommandRequest.Type, (byte)value.Type);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpCallCommandRequest.ActorUserId, value.ActorUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpCallCommandRequest.Revision, value.Revision);
        if (value.Grant is { } grant)
        {
            writer.WriteNested<TcpCallGrantSchemaEncoder, TcpCallGrant>(CoreCommandFieldNumbers.TcpCallCommandRequest.Grant, in grant);
        }

        if (value.Sdp is { } sdp)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpCallCommandRequest.Sdp, sdp);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.TcpCallCommandRequest.ClientOccurredAtMs, value.ClientOccurredAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpCallSignal))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallSignal.SignalId, nameof(TcpCallSignal.SignalId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallSignal.CallId, nameof(TcpCallSignal.CallId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallSignal.FromUserId, nameof(TcpCallSignal.FromUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallSignal.ToUserId, nameof(TcpCallSignal.ToUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallSignal.Kind, nameof(TcpCallSignal.Kind))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallSignal.Sdp, nameof(TcpCallSignal.Sdp))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallSignal.Revision, nameof(TcpCallSignal.Revision))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallSignal.OccurredAtMs, nameof(TcpCallSignal.OccurredAtMs))]
public static partial class TcpCallSignalSchema
{
    public static BinaryStatus TryEncode(
        in TcpCallSignal value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpCallSignalSchemaEncoder, TcpCallSignal>(in value, destination, limits, out written);
}

public readonly struct TcpCallSignalSchemaEncoder : IBinaryEncoder<TcpCallSignalSchemaEncoder, TcpCallSignal>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpCallSignal value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpCallSignal.SignalId, value.SignalId);
        writer.WriteString(CoreCommandFieldNumbers.TcpCallSignal.CallId, value.CallId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpCallSignal.FromUserId, value.FromUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpCallSignal.ToUserId, value.ToUserId);
        writer.WriteUInt32(CoreCommandFieldNumbers.TcpCallSignal.Kind, (byte)value.Kind);
        writer.WriteString(CoreCommandFieldNumbers.TcpCallSignal.Sdp, value.Sdp);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpCallSignal.Revision, value.Revision);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpCallSignal.OccurredAtMs, value.OccurredAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpCallCommandResponse))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.TcpCallCommandResponse.SignalToForward, nameof(TcpCallCommandResponse.SignalToForward), typeof(TcpCallSignalSchema))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandResponse.RequestId, nameof(TcpCallCommandResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandResponse.CallId, nameof(TcpCallCommandResponse.CallId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandResponse.Succeeded, nameof(TcpCallCommandResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandResponse.ErrorCode, nameof(TcpCallCommandResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandResponse.ErrorMessage, nameof(TcpCallCommandResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandResponse.State, nameof(TcpCallCommandResponse.State))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandResponse.EndReason, nameof(TcpCallCommandResponse.EndReason))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandResponse.Revision, nameof(TcpCallCommandResponse.Revision))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCallCommandResponse.Replayed, nameof(TcpCallCommandResponse.Replayed))]
public static partial class TcpCallCommandResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpCallCommandResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpCallCommandResponseSchemaEncoder, TcpCallCommandResponse>(in value, destination, limits, out written);
}

public readonly struct TcpCallCommandResponseSchemaEncoder : IBinaryEncoder<TcpCallCommandResponseSchemaEncoder, TcpCallCommandResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpCallCommandResponse value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpCallCommandResponse.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.TcpCallCommandResponse.CallId, value.CallId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpCallCommandResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpCallCommandResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpCallCommandResponse.ErrorMessage, errorMessage);
        }

        writer.WriteUInt32(CoreCommandFieldNumbers.TcpCallCommandResponse.State, (byte)value.State);
        writer.WriteUInt32(CoreCommandFieldNumbers.TcpCallCommandResponse.EndReason, (byte)value.EndReason);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpCallCommandResponse.Revision, value.Revision);
        writer.WriteBool(CoreCommandFieldNumbers.TcpCallCommandResponse.Replayed, value.Replayed);
        if (value.SignalToForward is { } signalToForward)
        {
            writer.WriteNested<TcpCallSignalSchemaEncoder, TcpCallSignal>(CoreCommandFieldNumbers.TcpCallCommandResponse.SignalToForward, in signalToForward);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageEditRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditRequest.RequestId, nameof(MessageEditRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditRequest.MessageId, nameof(MessageEditRequest.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditRequest.Content, nameof(MessageEditRequest.Content))]
public static partial class MessageEditRequestSchema
{
    public static BinaryStatus TryEncode(
        in MessageEditRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageEditRequestSchemaEncoder, MessageEditRequest>(in value, destination, limits, out written);
}

public readonly struct MessageEditRequestSchemaEncoder : IBinaryEncoder<MessageEditRequestSchemaEncoder, MessageEditRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageEditRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageEditRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.MessageEditRequest.MessageId, value.MessageId);
        writer.WriteString(CoreCommandFieldNumbers.MessageEditRequest.Content, value.Content);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageEditAcknowledgement))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditAcknowledgement.RequestId, nameof(MessageEditAcknowledgement.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditAcknowledgement.MessageId, nameof(MessageEditAcknowledgement.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditAcknowledgement.Succeeded, nameof(MessageEditAcknowledgement.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditAcknowledgement.ErrorCode, nameof(MessageEditAcknowledgement.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditAcknowledgement.ErrorMessage, nameof(MessageEditAcknowledgement.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditAcknowledgement.ConversationId, nameof(MessageEditAcknowledgement.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditAcknowledgement.Content, nameof(MessageEditAcknowledgement.Content))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditAcknowledgement.EditVersion, nameof(MessageEditAcknowledgement.EditVersion))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditAcknowledgement.EditedAtMs, nameof(MessageEditAcknowledgement.EditedAtMs))]
public static partial class MessageEditAcknowledgementSchema
{
    public static BinaryStatus TryEncode(
        in MessageEditAcknowledgement value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageEditAcknowledgementSchemaEncoder, MessageEditAcknowledgement>(in value, destination, limits, out written);
}

public readonly struct MessageEditAcknowledgementSchemaEncoder : IBinaryEncoder<MessageEditAcknowledgementSchemaEncoder, MessageEditAcknowledgement>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageEditAcknowledgement value)
    {
        writer.WriteString(CoreCommandFieldNumbers.MessageEditAcknowledgement.RequestId, value.RequestId);
        if (value.MessageId is { } messageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageEditAcknowledgement.MessageId, messageId);
        }

        writer.WriteBool(CoreCommandFieldNumbers.MessageEditAcknowledgement.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageEditAcknowledgement.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageEditAcknowledgement.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageEditAcknowledgement.ConversationId, conversationId);
        }

        if (value.Content is { } content)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageEditAcknowledgement.Content, content);
        }

        if (value.EditVersion is { } editVersion)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.MessageEditAcknowledgement.EditVersion, editVersion);
        }

        if (value.EditedAtMs is { } editedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageEditAcknowledgement.EditedAtMs, editedAtMs);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageEditedUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditedUpdate.MessageId, nameof(MessageEditedUpdate.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditedUpdate.ConversationId, nameof(MessageEditedUpdate.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditedUpdate.SenderUserId, nameof(MessageEditedUpdate.SenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditedUpdate.ReceiverUserId, nameof(MessageEditedUpdate.ReceiverUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditedUpdate.Content, nameof(MessageEditedUpdate.Content))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditedUpdate.EditVersion, nameof(MessageEditedUpdate.EditVersion))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageEditedUpdate.EditedAtMs, nameof(MessageEditedUpdate.EditedAtMs))]
public static partial class MessageEditedUpdateSchema
{
    public static BinaryStatus TryEncode(
        in MessageEditedUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageEditedUpdateSchemaEncoder, MessageEditedUpdate>(in value, destination, limits, out written);
}

public readonly struct MessageEditedUpdateSchemaEncoder : IBinaryEncoder<MessageEditedUpdateSchemaEncoder, MessageEditedUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageEditedUpdate value)
    {
        writer.WriteString(CoreCommandFieldNumbers.MessageEditedUpdate.MessageId, value.MessageId);
        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageEditedUpdate.ConversationId, conversationId);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.MessageEditedUpdate.SenderUserId, value.SenderUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.MessageEditedUpdate.ReceiverUserId, value.ReceiverUserId);
        writer.WriteString(CoreCommandFieldNumbers.MessageEditedUpdate.Content, value.Content);
        writer.WriteInt32(CoreCommandFieldNumbers.MessageEditedUpdate.EditVersion, value.EditVersion);
        writer.WriteInt64(CoreCommandFieldNumbers.MessageEditedUpdate.EditedAtMs, value.EditedAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageRecallRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecallRequest.RequestId, nameof(MessageRecallRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecallRequest.MessageId, nameof(MessageRecallRequest.MessageId))]
public static partial class MessageRecallRequestSchema
{
    public static BinaryStatus TryEncode(
        in MessageRecallRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageRecallRequestSchemaEncoder, MessageRecallRequest>(in value, destination, limits, out written);
}

public readonly struct MessageRecallRequestSchemaEncoder : IBinaryEncoder<MessageRecallRequestSchemaEncoder, MessageRecallRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageRecallRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageRecallRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.MessageRecallRequest.MessageId, value.MessageId);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageRecallAcknowledgement))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecallAcknowledgement.RequestId, nameof(MessageRecallAcknowledgement.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecallAcknowledgement.MessageId, nameof(MessageRecallAcknowledgement.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecallAcknowledgement.Succeeded, nameof(MessageRecallAcknowledgement.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecallAcknowledgement.ErrorCode, nameof(MessageRecallAcknowledgement.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecallAcknowledgement.ErrorMessage, nameof(MessageRecallAcknowledgement.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecallAcknowledgement.ConversationId, nameof(MessageRecallAcknowledgement.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecallAcknowledgement.RecalledAtMs, nameof(MessageRecallAcknowledgement.RecalledAtMs))]
public static partial class MessageRecallAcknowledgementSchema
{
    public static BinaryStatus TryEncode(
        in MessageRecallAcknowledgement value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageRecallAcknowledgementSchemaEncoder, MessageRecallAcknowledgement>(in value, destination, limits, out written);
}

public readonly struct MessageRecallAcknowledgementSchemaEncoder : IBinaryEncoder<MessageRecallAcknowledgementSchemaEncoder, MessageRecallAcknowledgement>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageRecallAcknowledgement value)
    {
        writer.WriteString(CoreCommandFieldNumbers.MessageRecallAcknowledgement.RequestId, value.RequestId);
        if (value.MessageId is { } messageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageRecallAcknowledgement.MessageId, messageId);
        }

        writer.WriteBool(CoreCommandFieldNumbers.MessageRecallAcknowledgement.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageRecallAcknowledgement.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageRecallAcknowledgement.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageRecallAcknowledgement.ConversationId, conversationId);
        }

        if (value.RecalledAtMs is { } recalledAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageRecallAcknowledgement.RecalledAtMs, recalledAtMs);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageRecalledUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecalledUpdate.MessageId, nameof(MessageRecalledUpdate.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecalledUpdate.ConversationId, nameof(MessageRecalledUpdate.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecalledUpdate.SenderUserId, nameof(MessageRecalledUpdate.SenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecalledUpdate.ReceiverUserId, nameof(MessageRecalledUpdate.ReceiverUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageRecalledUpdate.RecalledAtMs, nameof(MessageRecalledUpdate.RecalledAtMs))]
public static partial class MessageRecalledUpdateSchema
{
    public static BinaryStatus TryEncode(
        in MessageRecalledUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageRecalledUpdateSchemaEncoder, MessageRecalledUpdate>(in value, destination, limits, out written);
}

public readonly struct MessageRecalledUpdateSchemaEncoder : IBinaryEncoder<MessageRecalledUpdateSchemaEncoder, MessageRecalledUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageRecalledUpdate value)
    {
        writer.WriteString(CoreCommandFieldNumbers.MessageRecalledUpdate.MessageId, value.MessageId);
        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageRecalledUpdate.ConversationId, conversationId);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.MessageRecalledUpdate.SenderUserId, value.SenderUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.MessageRecalledUpdate.ReceiverUserId, value.ReceiverUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.MessageRecalledUpdate.RecalledAtMs, value.RecalledAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(AddReactionRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.AddReactionRequest.RequestId, nameof(AddReactionRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.AddReactionRequest.MessageId, nameof(AddReactionRequest.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.AddReactionRequest.Emoji, nameof(AddReactionRequest.Emoji))]
public static partial class AddReactionRequestSchema
{
    public static BinaryStatus TryEncode(
        in AddReactionRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<AddReactionRequestSchemaEncoder, AddReactionRequest>(in value, destination, limits, out written);
}

public readonly struct AddReactionRequestSchemaEncoder : IBinaryEncoder<AddReactionRequestSchemaEncoder, AddReactionRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in AddReactionRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.AddReactionRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.AddReactionRequest.MessageId, value.MessageId);
        writer.WriteString(CoreCommandFieldNumbers.AddReactionRequest.Emoji, value.Emoji);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(AddReactionAcknowledgement))]
[TcpBinaryField(CoreCommandFieldNumbers.AddReactionAcknowledgement.RequestId, nameof(AddReactionAcknowledgement.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.AddReactionAcknowledgement.MessageId, nameof(AddReactionAcknowledgement.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.AddReactionAcknowledgement.Succeeded, nameof(AddReactionAcknowledgement.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.AddReactionAcknowledgement.ErrorCode, nameof(AddReactionAcknowledgement.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.AddReactionAcknowledgement.ErrorMessage, nameof(AddReactionAcknowledgement.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.AddReactionAcknowledgement.ConversationId, nameof(AddReactionAcknowledgement.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.AddReactionAcknowledgement.Emoji, nameof(AddReactionAcknowledgement.Emoji))]
[TcpBinaryField(CoreCommandFieldNumbers.AddReactionAcknowledgement.OccurredAtMs, nameof(AddReactionAcknowledgement.OccurredAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.AddReactionAcknowledgement.EmojiCount, nameof(AddReactionAcknowledgement.EmojiCount))]
public static partial class AddReactionAcknowledgementSchema
{
    public static BinaryStatus TryEncode(
        in AddReactionAcknowledgement value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<AddReactionAcknowledgementSchemaEncoder, AddReactionAcknowledgement>(in value, destination, limits, out written);
}

public readonly struct AddReactionAcknowledgementSchemaEncoder : IBinaryEncoder<AddReactionAcknowledgementSchemaEncoder, AddReactionAcknowledgement>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in AddReactionAcknowledgement value)
    {
        writer.WriteString(CoreCommandFieldNumbers.AddReactionAcknowledgement.RequestId, value.RequestId);
        if (value.MessageId is { } messageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.AddReactionAcknowledgement.MessageId, messageId);
        }

        writer.WriteBool(CoreCommandFieldNumbers.AddReactionAcknowledgement.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.AddReactionAcknowledgement.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.AddReactionAcknowledgement.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.AddReactionAcknowledgement.ConversationId, conversationId);
        }

        if (value.Emoji is { } emoji)
        {
            writer.WriteString(CoreCommandFieldNumbers.AddReactionAcknowledgement.Emoji, emoji);
        }

        if (value.OccurredAtMs is { } occurredAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.AddReactionAcknowledgement.OccurredAtMs, occurredAtMs);
        }

        if (value.EmojiCount is { } emojiCount)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.AddReactionAcknowledgement.EmojiCount, emojiCount);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ReactionAddedUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionAddedUpdate.MessageId, nameof(ReactionAddedUpdate.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionAddedUpdate.ConversationId, nameof(ReactionAddedUpdate.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionAddedUpdate.ReactorUserId, nameof(ReactionAddedUpdate.ReactorUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionAddedUpdate.MessageSenderUserId, nameof(ReactionAddedUpdate.MessageSenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionAddedUpdate.MessageReceiverUserId, nameof(ReactionAddedUpdate.MessageReceiverUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionAddedUpdate.Emoji, nameof(ReactionAddedUpdate.Emoji))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionAddedUpdate.EmojiCount, nameof(ReactionAddedUpdate.EmojiCount))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionAddedUpdate.OccurredAtMs, nameof(ReactionAddedUpdate.OccurredAtMs))]
public static partial class ReactionAddedUpdateSchema
{
    public static BinaryStatus TryEncode(
        in ReactionAddedUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ReactionAddedUpdateSchemaEncoder, ReactionAddedUpdate>(in value, destination, limits, out written);
}

public readonly struct ReactionAddedUpdateSchemaEncoder : IBinaryEncoder<ReactionAddedUpdateSchemaEncoder, ReactionAddedUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ReactionAddedUpdate value)
    {
        writer.WriteString(CoreCommandFieldNumbers.ReactionAddedUpdate.MessageId, value.MessageId);
        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ReactionAddedUpdate.ConversationId, conversationId);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.ReactionAddedUpdate.ReactorUserId, value.ReactorUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.ReactionAddedUpdate.MessageSenderUserId, value.MessageSenderUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.ReactionAddedUpdate.MessageReceiverUserId, value.MessageReceiverUserId);
        writer.WriteString(CoreCommandFieldNumbers.ReactionAddedUpdate.Emoji, value.Emoji);
        writer.WriteInt32(CoreCommandFieldNumbers.ReactionAddedUpdate.EmojiCount, value.EmojiCount);
        writer.WriteInt64(CoreCommandFieldNumbers.ReactionAddedUpdate.OccurredAtMs, value.OccurredAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(RemoveReactionRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.RemoveReactionRequest.RequestId, nameof(RemoveReactionRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.RemoveReactionRequest.MessageId, nameof(RemoveReactionRequest.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.RemoveReactionRequest.Emoji, nameof(RemoveReactionRequest.Emoji))]
public static partial class RemoveReactionRequestSchema
{
    public static BinaryStatus TryEncode(
        in RemoveReactionRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<RemoveReactionRequestSchemaEncoder, RemoveReactionRequest>(in value, destination, limits, out written);
}

public readonly struct RemoveReactionRequestSchemaEncoder : IBinaryEncoder<RemoveReactionRequestSchemaEncoder, RemoveReactionRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in RemoveReactionRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.RemoveReactionRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.RemoveReactionRequest.MessageId, value.MessageId);
        writer.WriteString(CoreCommandFieldNumbers.RemoveReactionRequest.Emoji, value.Emoji);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(RemoveReactionAcknowledgement))]
[TcpBinaryField(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.RequestId, nameof(RemoveReactionAcknowledgement.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.MessageId, nameof(RemoveReactionAcknowledgement.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.Succeeded, nameof(RemoveReactionAcknowledgement.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.ErrorCode, nameof(RemoveReactionAcknowledgement.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.ErrorMessage, nameof(RemoveReactionAcknowledgement.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.ConversationId, nameof(RemoveReactionAcknowledgement.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.Emoji, nameof(RemoveReactionAcknowledgement.Emoji))]
[TcpBinaryField(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.OccurredAtMs, nameof(RemoveReactionAcknowledgement.OccurredAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.EmojiCount, nameof(RemoveReactionAcknowledgement.EmojiCount))]
public static partial class RemoveReactionAcknowledgementSchema
{
    public static BinaryStatus TryEncode(
        in RemoveReactionAcknowledgement value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<RemoveReactionAcknowledgementSchemaEncoder, RemoveReactionAcknowledgement>(in value, destination, limits, out written);
}

public readonly struct RemoveReactionAcknowledgementSchemaEncoder : IBinaryEncoder<RemoveReactionAcknowledgementSchemaEncoder, RemoveReactionAcknowledgement>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in RemoveReactionAcknowledgement value)
    {
        writer.WriteString(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.RequestId, value.RequestId);
        if (value.MessageId is { } messageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.MessageId, messageId);
        }

        writer.WriteBool(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.ConversationId, conversationId);
        }

        if (value.Emoji is { } emoji)
        {
            writer.WriteString(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.Emoji, emoji);
        }

        if (value.OccurredAtMs is { } occurredAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.OccurredAtMs, occurredAtMs);
        }

        if (value.EmojiCount is { } emojiCount)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.RemoveReactionAcknowledgement.EmojiCount, emojiCount);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ReactionRemovedUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionRemovedUpdate.MessageId, nameof(ReactionRemovedUpdate.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionRemovedUpdate.ConversationId, nameof(ReactionRemovedUpdate.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionRemovedUpdate.ReactorUserId, nameof(ReactionRemovedUpdate.ReactorUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionRemovedUpdate.MessageSenderUserId, nameof(ReactionRemovedUpdate.MessageSenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionRemovedUpdate.MessageReceiverUserId, nameof(ReactionRemovedUpdate.MessageReceiverUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionRemovedUpdate.Emoji, nameof(ReactionRemovedUpdate.Emoji))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionRemovedUpdate.EmojiCount, nameof(ReactionRemovedUpdate.EmojiCount))]
[TcpBinaryField(CoreCommandFieldNumbers.ReactionRemovedUpdate.OccurredAtMs, nameof(ReactionRemovedUpdate.OccurredAtMs))]
public static partial class ReactionRemovedUpdateSchema
{
    public static BinaryStatus TryEncode(
        in ReactionRemovedUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ReactionRemovedUpdateSchemaEncoder, ReactionRemovedUpdate>(in value, destination, limits, out written);
}

public readonly struct ReactionRemovedUpdateSchemaEncoder : IBinaryEncoder<ReactionRemovedUpdateSchemaEncoder, ReactionRemovedUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ReactionRemovedUpdate value)
    {
        writer.WriteString(CoreCommandFieldNumbers.ReactionRemovedUpdate.MessageId, value.MessageId);
        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ReactionRemovedUpdate.ConversationId, conversationId);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.ReactionRemovedUpdate.ReactorUserId, value.ReactorUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.ReactionRemovedUpdate.MessageSenderUserId, value.MessageSenderUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.ReactionRemovedUpdate.MessageReceiverUserId, value.MessageReceiverUserId);
        writer.WriteString(CoreCommandFieldNumbers.ReactionRemovedUpdate.Emoji, value.Emoji);
        writer.WriteInt32(CoreCommandFieldNumbers.ReactionRemovedUpdate.EmojiCount, value.EmojiCount);
        writer.WriteInt64(CoreCommandFieldNumbers.ReactionRemovedUpdate.OccurredAtMs, value.OccurredAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpConversationListItem))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.ConversationId, nameof(TcpConversationListItem.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.Type, nameof(TcpConversationListItem.Type))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.PeerUserId, nameof(TcpConversationListItem.PeerUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.Title, nameof(TcpConversationListItem.Title))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.LastMessageId, nameof(TcpConversationListItem.LastMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.LastMessagePreview, nameof(TcpConversationListItem.LastMessagePreview))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.LastMessageAtMs, nameof(TcpConversationListItem.LastMessageAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.LastSenderUserId, nameof(TcpConversationListItem.LastSenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.UnreadCount, nameof(TcpConversationListItem.UnreadCount))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.LastReadMessageId, nameof(TcpConversationListItem.LastReadMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.LastReadAtMs, nameof(TcpConversationListItem.LastReadAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.IsPinned, nameof(TcpConversationListItem.IsPinned))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.PinnedAtMs, nameof(TcpConversationListItem.PinnedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.IsMuted, nameof(TcpConversationListItem.IsMuted))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListItem.MutedUntilMs, nameof(TcpConversationListItem.MutedUntilMs))]
public static partial class TcpConversationListItemSchema
{
    public static BinaryStatus TryEncode(
        in TcpConversationListItem value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpConversationListItemSchemaEncoder, TcpConversationListItem>(in value, destination, limits, out written);
}

public readonly struct TcpConversationListItemSchemaEncoder : IBinaryEncoder<TcpConversationListItemSchemaEncoder, TcpConversationListItem>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpConversationListItem value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpConversationListItem.ConversationId, value.ConversationId);
        writer.WriteUInt32(CoreCommandFieldNumbers.TcpConversationListItem.Type, (byte)value.Type);
        if (value.PeerUserId is { } peerUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.TcpConversationListItem.PeerUserId, peerUserId);
        }

        if (value.Title is { } title)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpConversationListItem.Title, title);
        }

        if (value.LastMessageId is { } lastMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpConversationListItem.LastMessageId, lastMessageId);
        }

        if (value.LastMessagePreview is { } lastMessagePreview)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpConversationListItem.LastMessagePreview, lastMessagePreview);
        }

        if (value.LastMessageAtMs is { } lastMessageAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.TcpConversationListItem.LastMessageAtMs, lastMessageAtMs);
        }

        if (value.LastSenderUserId is { } lastSenderUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.TcpConversationListItem.LastSenderUserId, lastSenderUserId);
        }

        writer.WriteInt32(CoreCommandFieldNumbers.TcpConversationListItem.UnreadCount, value.UnreadCount);
        if (value.LastReadMessageId is { } lastReadMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpConversationListItem.LastReadMessageId, lastReadMessageId);
        }

        if (value.LastReadAtMs is { } lastReadAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.TcpConversationListItem.LastReadAtMs, lastReadAtMs);
        }

        writer.WriteBool(CoreCommandFieldNumbers.TcpConversationListItem.IsPinned, value.IsPinned);
        if (value.PinnedAtMs is { } pinnedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.TcpConversationListItem.PinnedAtMs, pinnedAtMs);
        }

        writer.WriteBool(CoreCommandFieldNumbers.TcpConversationListItem.IsMuted, value.IsMuted);
        if (value.MutedUntilMs is { } mutedUntilMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.TcpConversationListItem.MutedUntilMs, mutedUntilMs);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpConversationListCursor))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListCursor.IsPinned, nameof(TcpConversationListCursor.IsPinned))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListCursor.PinnedAtMs, nameof(TcpConversationListCursor.PinnedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListCursor.LastMessageAtMs, nameof(TcpConversationListCursor.LastMessageAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationListCursor.ConversationId, nameof(TcpConversationListCursor.ConversationId))]
public static partial class TcpConversationListCursorSchema
{
    public static BinaryStatus TryEncode(
        in TcpConversationListCursor value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpConversationListCursorSchemaEncoder, TcpConversationListCursor>(in value, destination, limits, out written);
}

public readonly struct TcpConversationListCursorSchemaEncoder : IBinaryEncoder<TcpConversationListCursorSchemaEncoder, TcpConversationListCursor>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpConversationListCursor value)
    {
        writer.WriteBool(CoreCommandFieldNumbers.TcpConversationListCursor.IsPinned, value.IsPinned);
        if (value.PinnedAtMs is { } pinnedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.TcpConversationListCursor.PinnedAtMs, pinnedAtMs);
        }

        if (value.LastMessageAtMs is { } lastMessageAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.TcpConversationListCursor.LastMessageAtMs, lastMessageAtMs);
        }

        writer.WriteString(CoreCommandFieldNumbers.TcpConversationListCursor.ConversationId, value.ConversationId);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ConversationListRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationListRequest.RequestId, nameof(ConversationListRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationListRequest.BeforeIsPinned, nameof(ConversationListRequest.BeforeIsPinned))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationListRequest.BeforePinnedAtMs, nameof(ConversationListRequest.BeforePinnedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationListRequest.BeforeLastMessageAtMs, nameof(ConversationListRequest.BeforeLastMessageAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationListRequest.BeforeConversationId, nameof(ConversationListRequest.BeforeConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationListRequest.Limit, nameof(ConversationListRequest.Limit))]
public static partial class ConversationListRequestSchema
{
    public static BinaryStatus TryEncode(
        in ConversationListRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ConversationListRequestSchemaEncoder, ConversationListRequest>(in value, destination, limits, out written);
}

public readonly struct ConversationListRequestSchemaEncoder : IBinaryEncoder<ConversationListRequestSchemaEncoder, ConversationListRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ConversationListRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationListRequest.RequestId, requestId);
        }

        if (value.BeforeIsPinned is { } beforeIsPinned)
        {
            writer.WriteBool(CoreCommandFieldNumbers.ConversationListRequest.BeforeIsPinned, beforeIsPinned);
        }

        if (value.BeforePinnedAtMs is { } beforePinnedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ConversationListRequest.BeforePinnedAtMs, beforePinnedAtMs);
        }

        if (value.BeforeLastMessageAtMs is { } beforeLastMessageAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ConversationListRequest.BeforeLastMessageAtMs, beforeLastMessageAtMs);
        }

        if (value.BeforeConversationId is { } beforeConversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationListRequest.BeforeConversationId, beforeConversationId);
        }

        writer.WriteInt32(CoreCommandFieldNumbers.ConversationListRequest.Limit, value.Limit);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ConversationListPage))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationListPage.RequestId, nameof(ConversationListPage.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationListPage.Succeeded, nameof(ConversationListPage.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationListPage.ErrorCode, nameof(ConversationListPage.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationListPage.ErrorMessage, nameof(ConversationListPage.ErrorMessage))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.ConversationListPage.Items, nameof(ConversationListPage.Items), typeof(TcpConversationListItemSchema))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.ConversationListPage.NextCursor, nameof(ConversationListPage.NextCursor), typeof(TcpConversationListCursorSchema))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationListPage.HasMore, nameof(ConversationListPage.HasMore))]
public static partial class ConversationListPageSchema
{
    public static BinaryStatus TryEncode(
        in ConversationListPage value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ConversationListPageSchemaEncoder, ConversationListPage>(in value, destination, limits, out written);
}

public readonly struct ConversationListPageSchemaEncoder : IBinaryEncoder<ConversationListPageSchemaEncoder, ConversationListPage>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ConversationListPage value)
    {
        writer.WriteString(CoreCommandFieldNumbers.ConversationListPage.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.ConversationListPage.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationListPage.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationListPage.ErrorMessage, errorMessage);
        }

        if (value.Items is { } items)
        {
            foreach (TcpConversationListItem item in items)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.ConversationListPage.Items)) return writer.Status;
                writer.WriteNested<TcpConversationListItemSchemaEncoder, TcpConversationListItem>(CoreCommandFieldNumbers.ConversationListPage.Items, in item, allowRepeatedFieldNumber: true);
            }
        }

        if (value.NextCursor is { } nextCursor)
        {
            writer.WriteNested<TcpConversationListCursorSchemaEncoder, TcpConversationListCursor>(CoreCommandFieldNumbers.ConversationListPage.NextCursor, in nextCursor);
        }

        writer.WriteBool(CoreCommandFieldNumbers.ConversationListPage.HasMore, value.HasMore);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ConversationMarkReadRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadRequest.RequestId, nameof(ConversationMarkReadRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadRequest.ConversationId, nameof(ConversationMarkReadRequest.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadRequest.ReadAtMs, nameof(ConversationMarkReadRequest.ReadAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadRequest.ReadMessageId, nameof(ConversationMarkReadRequest.ReadMessageId))]
public static partial class ConversationMarkReadRequestSchema
{
    public static BinaryStatus TryEncode(
        in ConversationMarkReadRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ConversationMarkReadRequestSchemaEncoder, ConversationMarkReadRequest>(in value, destination, limits, out written);
}

public readonly struct ConversationMarkReadRequestSchemaEncoder : IBinaryEncoder<ConversationMarkReadRequestSchemaEncoder, ConversationMarkReadRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ConversationMarkReadRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationMarkReadRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.ConversationMarkReadRequest.ConversationId, value.ConversationId);
        if (value.ReadAtMs is { } readAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ConversationMarkReadRequest.ReadAtMs, readAtMs);
        }

        if (value.ReadMessageId is { } readMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationMarkReadRequest.ReadMessageId, readMessageId);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ConversationMarkReadResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadResponse.RequestId, nameof(ConversationMarkReadResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadResponse.Succeeded, nameof(ConversationMarkReadResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadResponse.ErrorCode, nameof(ConversationMarkReadResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadResponse.ErrorMessage, nameof(ConversationMarkReadResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadResponse.ConversationId, nameof(ConversationMarkReadResponse.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadResponse.UnreadCount, nameof(ConversationMarkReadResponse.UnreadCount))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadResponse.LastReadMessageId, nameof(ConversationMarkReadResponse.LastReadMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadResponse.LastReadAtMs, nameof(ConversationMarkReadResponse.LastReadAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationMarkReadResponse.Changed, nameof(ConversationMarkReadResponse.Changed))]
public static partial class ConversationMarkReadResponseSchema
{
    public static BinaryStatus TryEncode(
        in ConversationMarkReadResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ConversationMarkReadResponseSchemaEncoder, ConversationMarkReadResponse>(in value, destination, limits, out written);
}

public readonly struct ConversationMarkReadResponseSchemaEncoder : IBinaryEncoder<ConversationMarkReadResponseSchemaEncoder, ConversationMarkReadResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ConversationMarkReadResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.ConversationMarkReadResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.ConversationMarkReadResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationMarkReadResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationMarkReadResponse.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationMarkReadResponse.ConversationId, conversationId);
        }

        writer.WriteInt32(CoreCommandFieldNumbers.ConversationMarkReadResponse.UnreadCount, value.UnreadCount);
        if (value.LastReadMessageId is { } lastReadMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationMarkReadResponse.LastReadMessageId, lastReadMessageId);
        }

        if (value.LastReadAtMs is { } lastReadAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ConversationMarkReadResponse.LastReadAtMs, lastReadAtMs);
        }

        writer.WriteBool(CoreCommandFieldNumbers.ConversationMarkReadResponse.Changed, value.Changed);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ConversationChangedUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationChangedUpdate.ConversationId, nameof(ConversationChangedUpdate.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationChangedUpdate.Type, nameof(ConversationChangedUpdate.Type))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationChangedUpdate.PeerUserId, nameof(ConversationChangedUpdate.PeerUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationChangedUpdate.Title, nameof(ConversationChangedUpdate.Title))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationChangedUpdate.LastMessageId, nameof(ConversationChangedUpdate.LastMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationChangedUpdate.LastMessagePreview, nameof(ConversationChangedUpdate.LastMessagePreview))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationChangedUpdate.LastMessageAtMs, nameof(ConversationChangedUpdate.LastMessageAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationChangedUpdate.LastSenderUserId, nameof(ConversationChangedUpdate.LastSenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationChangedUpdate.IsPinned, nameof(ConversationChangedUpdate.IsPinned))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationChangedUpdate.IsMuted, nameof(ConversationChangedUpdate.IsMuted))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationChangedUpdate.MutedUntilMs, nameof(ConversationChangedUpdate.MutedUntilMs))]
public static partial class ConversationChangedUpdateSchema
{
    public static BinaryStatus TryEncode(
        in ConversationChangedUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ConversationChangedUpdateSchemaEncoder, ConversationChangedUpdate>(in value, destination, limits, out written);
}

public readonly struct ConversationChangedUpdateSchemaEncoder : IBinaryEncoder<ConversationChangedUpdateSchemaEncoder, ConversationChangedUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ConversationChangedUpdate value)
    {
        writer.WriteString(CoreCommandFieldNumbers.ConversationChangedUpdate.ConversationId, value.ConversationId);
        writer.WriteUInt32(CoreCommandFieldNumbers.ConversationChangedUpdate.Type, (byte)value.Type);
        if (value.PeerUserId is { } peerUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ConversationChangedUpdate.PeerUserId, peerUserId);
        }

        if (value.Title is { } title)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationChangedUpdate.Title, title);
        }

        if (value.LastMessageId is { } lastMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationChangedUpdate.LastMessageId, lastMessageId);
        }

        if (value.LastMessagePreview is { } lastMessagePreview)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationChangedUpdate.LastMessagePreview, lastMessagePreview);
        }

        if (value.LastMessageAtMs is { } lastMessageAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ConversationChangedUpdate.LastMessageAtMs, lastMessageAtMs);
        }

        if (value.LastSenderUserId is { } lastSenderUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ConversationChangedUpdate.LastSenderUserId, lastSenderUserId);
        }

        if (value.IsPinned is { } isPinned)
        {
            writer.WriteBool(CoreCommandFieldNumbers.ConversationChangedUpdate.IsPinned, isPinned);
        }

        if (value.IsMuted is { } isMuted)
        {
            writer.WriteBool(CoreCommandFieldNumbers.ConversationChangedUpdate.IsMuted, isMuted);
        }

        if (value.MutedUntilMs is { } mutedUntilMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ConversationChangedUpdate.MutedUntilMs, mutedUntilMs);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(UnreadCountChanged))]
[TcpBinaryField(CoreCommandFieldNumbers.UnreadCountChanged.ConversationId, nameof(UnreadCountChanged.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.UnreadCountChanged.UnreadCount, nameof(UnreadCountChanged.UnreadCount))]
[TcpBinaryField(CoreCommandFieldNumbers.UnreadCountChanged.LastReadMessageId, nameof(UnreadCountChanged.LastReadMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.UnreadCountChanged.LastReadAtMs, nameof(UnreadCountChanged.LastReadAtMs))]
public static partial class UnreadCountChangedSchema
{
    public static BinaryStatus TryEncode(
        in UnreadCountChanged value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<UnreadCountChangedSchemaEncoder, UnreadCountChanged>(in value, destination, limits, out written);
}

public readonly struct UnreadCountChangedSchemaEncoder : IBinaryEncoder<UnreadCountChangedSchemaEncoder, UnreadCountChanged>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in UnreadCountChanged value)
    {
        writer.WriteString(CoreCommandFieldNumbers.UnreadCountChanged.ConversationId, value.ConversationId);
        writer.WriteInt32(CoreCommandFieldNumbers.UnreadCountChanged.UnreadCount, value.UnreadCount);
        if (value.LastReadMessageId is { } lastReadMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.UnreadCountChanged.LastReadMessageId, lastReadMessageId);
        }

        if (value.LastReadAtMs is { } lastReadAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.UnreadCountChanged.LastReadAtMs, lastReadAtMs);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ConversationSetPrefsRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsRequest.RequestId, nameof(ConversationSetPrefsRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsRequest.ConversationId, nameof(ConversationSetPrefsRequest.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsRequest.Pinned, nameof(ConversationSetPrefsRequest.Pinned))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsRequest.Muted, nameof(ConversationSetPrefsRequest.Muted))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsRequest.MutedUntilMs, nameof(ConversationSetPrefsRequest.MutedUntilMs))]
public static partial class ConversationSetPrefsRequestSchema
{
    public static BinaryStatus TryEncode(
        in ConversationSetPrefsRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ConversationSetPrefsRequestSchemaEncoder, ConversationSetPrefsRequest>(in value, destination, limits, out written);
}

public readonly struct ConversationSetPrefsRequestSchemaEncoder : IBinaryEncoder<ConversationSetPrefsRequestSchemaEncoder, ConversationSetPrefsRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ConversationSetPrefsRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationSetPrefsRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.ConversationSetPrefsRequest.ConversationId, value.ConversationId);
        if (value.Pinned is { } pinned)
        {
            writer.WriteBool(CoreCommandFieldNumbers.ConversationSetPrefsRequest.Pinned, pinned);
        }

        if (value.Muted is { } muted)
        {
            writer.WriteBool(CoreCommandFieldNumbers.ConversationSetPrefsRequest.Muted, muted);
        }

        if (value.MutedUntilMs is { } mutedUntilMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ConversationSetPrefsRequest.MutedUntilMs, mutedUntilMs);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ConversationSetPrefsResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsResponse.RequestId, nameof(ConversationSetPrefsResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsResponse.Succeeded, nameof(ConversationSetPrefsResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsResponse.ErrorCode, nameof(ConversationSetPrefsResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsResponse.ErrorMessage, nameof(ConversationSetPrefsResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsResponse.ConversationId, nameof(ConversationSetPrefsResponse.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsResponse.IsPinned, nameof(ConversationSetPrefsResponse.IsPinned))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsResponse.IsMuted, nameof(ConversationSetPrefsResponse.IsMuted))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsResponse.MutedUntilMs, nameof(ConversationSetPrefsResponse.MutedUntilMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSetPrefsResponse.Changed, nameof(ConversationSetPrefsResponse.Changed))]
public static partial class ConversationSetPrefsResponseSchema
{
    public static BinaryStatus TryEncode(
        in ConversationSetPrefsResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ConversationSetPrefsResponseSchemaEncoder, ConversationSetPrefsResponse>(in value, destination, limits, out written);
}

public readonly struct ConversationSetPrefsResponseSchemaEncoder : IBinaryEncoder<ConversationSetPrefsResponseSchemaEncoder, ConversationSetPrefsResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ConversationSetPrefsResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.ConversationSetPrefsResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.ConversationSetPrefsResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationSetPrefsResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationSetPrefsResponse.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ConversationSetPrefsResponse.ConversationId, conversationId);
        }

        writer.WriteBool(CoreCommandFieldNumbers.ConversationSetPrefsResponse.IsPinned, value.IsPinned);
        writer.WriteBool(CoreCommandFieldNumbers.ConversationSetPrefsResponse.IsMuted, value.IsMuted);
        if (value.MutedUntilMs is { } mutedUntilMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ConversationSetPrefsResponse.MutedUntilMs, mutedUntilMs);
        }

        writer.WriteBool(CoreCommandFieldNumbers.ConversationSetPrefsResponse.Changed, value.Changed);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ConversationReadUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationReadUpdate.ConversationId, nameof(ConversationReadUpdate.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationReadUpdate.ReaderUserId, nameof(ConversationReadUpdate.ReaderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationReadUpdate.LastReadMessageId, nameof(ConversationReadUpdate.LastReadMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationReadUpdate.LastReadAtMs, nameof(ConversationReadUpdate.LastReadAtMs))]
public static partial class ConversationReadUpdateSchema
{
    public static BinaryStatus TryEncode(
        in ConversationReadUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ConversationReadUpdateSchemaEncoder, ConversationReadUpdate>(in value, destination, limits, out written);
}

public readonly struct ConversationReadUpdateSchemaEncoder : IBinaryEncoder<ConversationReadUpdateSchemaEncoder, ConversationReadUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ConversationReadUpdate value)
    {
        writer.WriteString(CoreCommandFieldNumbers.ConversationReadUpdate.ConversationId, value.ConversationId);
        writer.WriteInt64(CoreCommandFieldNumbers.ConversationReadUpdate.ReaderUserId, value.ReaderUserId);
        writer.WriteString(CoreCommandFieldNumbers.ConversationReadUpdate.LastReadMessageId, value.LastReadMessageId);
        writer.WriteInt64(CoreCommandFieldNumbers.ConversationReadUpdate.LastReadAtMs, value.LastReadAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageReadReceiptItem))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptItem.UserId, nameof(MessageReadReceiptItem.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptItem.ReadAtMs, nameof(MessageReadReceiptItem.ReadAtMs))]
public static partial class MessageReadReceiptItemSchema
{
    public static BinaryStatus TryEncode(
        in MessageReadReceiptItem value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageReadReceiptItemSchemaEncoder, MessageReadReceiptItem>(in value, destination, limits, out written);
}

public readonly struct MessageReadReceiptItemSchemaEncoder : IBinaryEncoder<MessageReadReceiptItemSchemaEncoder, MessageReadReceiptItem>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageReadReceiptItem value)
    {
        writer.WriteInt64(CoreCommandFieldNumbers.MessageReadReceiptItem.UserId, value.UserId);
        writer.WriteInt64(CoreCommandFieldNumbers.MessageReadReceiptItem.ReadAtMs, value.ReadAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageReadReceiptQueryRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryRequest.RequestId, nameof(MessageReadReceiptQueryRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryRequest.ConversationId, nameof(MessageReadReceiptQueryRequest.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryRequest.MessageId, nameof(MessageReadReceiptQueryRequest.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryRequest.Cursor, nameof(MessageReadReceiptQueryRequest.Cursor))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryRequest.PageSize, nameof(MessageReadReceiptQueryRequest.PageSize))]
public static partial class MessageReadReceiptQueryRequestSchema
{
    public static BinaryStatus TryEncode(
        in MessageReadReceiptQueryRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageReadReceiptQueryRequestSchemaEncoder, MessageReadReceiptQueryRequest>(in value, destination, limits, out written);
}

public readonly struct MessageReadReceiptQueryRequestSchemaEncoder : IBinaryEncoder<MessageReadReceiptQueryRequestSchemaEncoder, MessageReadReceiptQueryRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageReadReceiptQueryRequest value)
    {
        writer.WriteString(CoreCommandFieldNumbers.MessageReadReceiptQueryRequest.RequestId, value.RequestId);
        writer.WriteString(CoreCommandFieldNumbers.MessageReadReceiptQueryRequest.ConversationId, value.ConversationId);
        writer.WriteString(CoreCommandFieldNumbers.MessageReadReceiptQueryRequest.MessageId, value.MessageId);
        if (value.Cursor is { } cursor)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageReadReceiptQueryRequest.Cursor, cursor);
        }

        writer.WriteInt32(CoreCommandFieldNumbers.MessageReadReceiptQueryRequest.PageSize, value.PageSize);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageReadReceiptQueryResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.RequestId, nameof(MessageReadReceiptQueryResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.Succeeded, nameof(MessageReadReceiptQueryResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.ErrorCode, nameof(MessageReadReceiptQueryResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.ErrorMessage, nameof(MessageReadReceiptQueryResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.ConversationId, nameof(MessageReadReceiptQueryResponse.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.ReadCount, nameof(MessageReadReceiptQueryResponse.ReadCount))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.TotalMemberCount, nameof(MessageReadReceiptQueryResponse.TotalMemberCount))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.IsSmallGroup, nameof(MessageReadReceiptQueryResponse.IsSmallGroup))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.Readers, nameof(MessageReadReceiptQueryResponse.Readers), typeof(MessageReadReceiptItemSchema))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.NextCursor, nameof(MessageReadReceiptQueryResponse.NextCursor))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.HasMore, nameof(MessageReadReceiptQueryResponse.HasMore))]
public static partial class MessageReadReceiptQueryResponseSchema
{
    public static BinaryStatus TryEncode(
        in MessageReadReceiptQueryResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageReadReceiptQueryResponseSchemaEncoder, MessageReadReceiptQueryResponse>(in value, destination, limits, out written);
}

public readonly struct MessageReadReceiptQueryResponseSchemaEncoder : IBinaryEncoder<MessageReadReceiptQueryResponseSchemaEncoder, MessageReadReceiptQueryResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageReadReceiptQueryResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.ConversationId, conversationId);
        }

        writer.WriteInt32(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.ReadCount, value.ReadCount);
        writer.WriteInt32(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.TotalMemberCount, value.TotalMemberCount);
        writer.WriteBool(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.IsSmallGroup, value.IsSmallGroup);
        if (value.Readers is { } readers)
        {
            foreach (MessageReadReceiptItem reader in readers)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.Readers)) return writer.Status;
                writer.WriteNested<MessageReadReceiptItemSchemaEncoder, MessageReadReceiptItem>(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.Readers, in reader, allowRepeatedFieldNumber: true);
            }
        }

        if (value.NextCursor is { } nextCursor)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.NextCursor, nextCursor);
        }

        writer.WriteBool(CoreCommandFieldNumbers.MessageReadReceiptQueryResponse.HasMore, value.HasMore);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ConversationSyncWatermark))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSyncWatermark.ConversationId, nameof(ConversationSyncWatermark.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSyncWatermark.AfterReceivedAtMs, nameof(ConversationSyncWatermark.AfterReceivedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationSyncWatermark.AfterMessageId, nameof(ConversationSyncWatermark.AfterMessageId))]
public static partial class ConversationSyncWatermarkSchema
{
    public static BinaryStatus TryEncode(
        in ConversationSyncWatermark value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ConversationSyncWatermarkSchemaEncoder, ConversationSyncWatermark>(in value, destination, limits, out written);
}

public readonly struct ConversationSyncWatermarkSchemaEncoder : IBinaryEncoder<ConversationSyncWatermarkSchemaEncoder, ConversationSyncWatermark>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ConversationSyncWatermark value)
    {
        writer.WriteString(CoreCommandFieldNumbers.ConversationSyncWatermark.ConversationId, value.ConversationId);
        writer.WriteInt64(CoreCommandFieldNumbers.ConversationSyncWatermark.AfterReceivedAtMs, value.AfterReceivedAtMs);
        writer.WriteString(CoreCommandFieldNumbers.ConversationSyncWatermark.AfterMessageId, value.AfterMessageId);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(RelationshipSyncWatermark))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipSyncWatermark.ListType, nameof(RelationshipSyncWatermark.ListType))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipSyncWatermark.AfterSequence, nameof(RelationshipSyncWatermark.AfterSequence))]
public static partial class RelationshipSyncWatermarkSchema
{
    public static BinaryStatus TryEncode(
        in RelationshipSyncWatermark value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<RelationshipSyncWatermarkSchemaEncoder, RelationshipSyncWatermark>(in value, destination, limits, out written);
}

public readonly struct RelationshipSyncWatermarkSchemaEncoder : IBinaryEncoder<RelationshipSyncWatermarkSchemaEncoder, RelationshipSyncWatermark>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in RelationshipSyncWatermark value)
    {
        writer.WriteUInt32(CoreCommandFieldNumbers.RelationshipSyncWatermark.ListType, (byte)value.ListType);
        writer.WriteInt64(CoreCommandFieldNumbers.RelationshipSyncWatermark.AfterSequence, value.AfterSequence);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ConversationHistoryCatchUp))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationHistoryCatchUp.ConversationId, nameof(ConversationHistoryCatchUp.ConversationId))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.ConversationHistoryCatchUp.Items, nameof(ConversationHistoryCatchUp.Items), typeof(MessageHistoryItemSchema))]
[TcpBinaryField(CoreCommandFieldNumbers.ConversationHistoryCatchUp.HasMore, nameof(ConversationHistoryCatchUp.HasMore))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.ConversationHistoryCatchUp.NextCursor, nameof(ConversationHistoryCatchUp.NextCursor), typeof(MessageHistoryCursorSchema))]
public static partial class ConversationHistoryCatchUpSchema
{
    public static BinaryStatus TryEncode(
        in ConversationHistoryCatchUp value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ConversationHistoryCatchUpSchemaEncoder, ConversationHistoryCatchUp>(in value, destination, limits, out written);
}

public readonly struct ConversationHistoryCatchUpSchemaEncoder : IBinaryEncoder<ConversationHistoryCatchUpSchemaEncoder, ConversationHistoryCatchUp>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ConversationHistoryCatchUp value)
    {
        writer.WriteString(CoreCommandFieldNumbers.ConversationHistoryCatchUp.ConversationId, value.ConversationId);
        if (value.Items is { } items)
        {
            foreach (MessageHistoryItem item in items)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.ConversationHistoryCatchUp.Items)) return writer.Status;
                writer.WriteNested<MessageHistoryItemSchemaEncoder, MessageHistoryItem>(CoreCommandFieldNumbers.ConversationHistoryCatchUp.Items, in item, allowRepeatedFieldNumber: true);
            }
        }

        writer.WriteBool(CoreCommandFieldNumbers.ConversationHistoryCatchUp.HasMore, value.HasMore);
        if (value.NextCursor is { } nextCursor)
        {
            writer.WriteNested<MessageHistoryCursorSchemaEncoder, MessageHistoryCursor>(CoreCommandFieldNumbers.ConversationHistoryCatchUp.NextCursor, in nextCursor);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(SyncCursorResetRequired))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncCursorResetRequired.ConversationId, nameof(SyncCursorResetRequired.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncCursorResetRequired.Reason, nameof(SyncCursorResetRequired.Reason))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncCursorResetRequired.TipMessageId, nameof(SyncCursorResetRequired.TipMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncCursorResetRequired.TipReceivedAtMs, nameof(SyncCursorResetRequired.TipReceivedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncCursorResetRequired.ClientAfterReceivedAtMs, nameof(SyncCursorResetRequired.ClientAfterReceivedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncCursorResetRequired.ClientAfterMessageId, nameof(SyncCursorResetRequired.ClientAfterMessageId))]
public static partial class SyncCursorResetRequiredSchema
{
    public static BinaryStatus TryEncode(
        in SyncCursorResetRequired value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<SyncCursorResetRequiredSchemaEncoder, SyncCursorResetRequired>(in value, destination, limits, out written);
}

public readonly struct SyncCursorResetRequiredSchemaEncoder : IBinaryEncoder<SyncCursorResetRequiredSchemaEncoder, SyncCursorResetRequired>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in SyncCursorResetRequired value)
    {
        writer.WriteString(CoreCommandFieldNumbers.SyncCursorResetRequired.ConversationId, value.ConversationId);
        writer.WriteUInt32(CoreCommandFieldNumbers.SyncCursorResetRequired.Reason, (byte)value.Reason);
        if (value.TipMessageId is { } tipMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.SyncCursorResetRequired.TipMessageId, tipMessageId);
        }

        if (value.TipReceivedAtMs is { } tipReceivedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.SyncCursorResetRequired.TipReceivedAtMs, tipReceivedAtMs);
        }

        if (value.ClientAfterReceivedAtMs is { } clientAfterReceivedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.SyncCursorResetRequired.ClientAfterReceivedAtMs, clientAfterReceivedAtMs);
        }

        if (value.ClientAfterMessageId is { } clientAfterMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.SyncCursorResetRequired.ClientAfterMessageId, clientAfterMessageId);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(RelationshipChangeLogEntry))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipChangeLogEntry.Operation, nameof(RelationshipChangeLogEntry.Operation))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipChangeLogEntry.ResourceId, nameof(RelationshipChangeLogEntry.ResourceId))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipChangeLogEntry.UserId, nameof(RelationshipChangeLogEntry.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipChangeLogEntry.Status, nameof(RelationshipChangeLogEntry.Status))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipChangeLogEntry.Message, nameof(RelationshipChangeLogEntry.Message))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipChangeLogEntry.CreatedAtMs, nameof(RelationshipChangeLogEntry.CreatedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipChangeLogEntry.OccurredAtMs, nameof(RelationshipChangeLogEntry.OccurredAtMs))]
public static partial class RelationshipChangeLogEntrySchema
{
    public static BinaryStatus TryEncode(
        in RelationshipChangeLogEntry value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<RelationshipChangeLogEntrySchemaEncoder, RelationshipChangeLogEntry>(in value, destination, limits, out written);
}

public readonly struct RelationshipChangeLogEntrySchemaEncoder : IBinaryEncoder<RelationshipChangeLogEntrySchemaEncoder, RelationshipChangeLogEntry>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in RelationshipChangeLogEntry value)
    {
        writer.WriteUInt32(CoreCommandFieldNumbers.RelationshipChangeLogEntry.Operation, (byte)value.Operation);
        writer.WriteString(CoreCommandFieldNumbers.RelationshipChangeLogEntry.ResourceId, value.ResourceId);
        writer.WriteInt64(CoreCommandFieldNumbers.RelationshipChangeLogEntry.UserId, value.UserId);
        if (value.Status is { } status)
        {
            writer.WriteString(CoreCommandFieldNumbers.RelationshipChangeLogEntry.Status, status);
        }

        if (value.Message is { } message)
        {
            writer.WriteString(CoreCommandFieldNumbers.RelationshipChangeLogEntry.Message, message);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.RelationshipChangeLogEntry.CreatedAtMs, value.CreatedAtMs);
        writer.WriteInt64(CoreCommandFieldNumbers.RelationshipChangeLogEntry.OccurredAtMs, value.OccurredAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(RelationshipCatchUp))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipCatchUp.ListType, nameof(RelationshipCatchUp.ListType))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.RelationshipCatchUp.Changes, nameof(RelationshipCatchUp.Changes), typeof(RelationshipChangeLogEntrySchema))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipCatchUp.HasMore, nameof(RelationshipCatchUp.HasMore))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipCatchUp.NextCursor, nameof(RelationshipCatchUp.NextCursor))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipCatchUp.NextSequence, nameof(RelationshipCatchUp.NextSequence))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipCatchUp.ResetRequired, nameof(RelationshipCatchUp.ResetRequired))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipCatchUp.ErrorCode, nameof(RelationshipCatchUp.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.RelationshipCatchUp.ErrorMessage, nameof(RelationshipCatchUp.ErrorMessage))]
public static partial class RelationshipCatchUpSchema
{
    public static BinaryStatus TryEncode(
        in RelationshipCatchUp value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<RelationshipCatchUpSchemaEncoder, RelationshipCatchUp>(in value, destination, limits, out written);
}

public readonly struct RelationshipCatchUpSchemaEncoder : IBinaryEncoder<RelationshipCatchUpSchemaEncoder, RelationshipCatchUp>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in RelationshipCatchUp value)
    {
        writer.WriteUInt32(CoreCommandFieldNumbers.RelationshipCatchUp.ListType, (byte)value.ListType);
        if (value.Changes is { } changes)
        {
            foreach (RelationshipChangeLogEntry change in changes)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.RelationshipCatchUp.Changes)) return writer.Status;
                writer.WriteNested<RelationshipChangeLogEntrySchemaEncoder, RelationshipChangeLogEntry>(CoreCommandFieldNumbers.RelationshipCatchUp.Changes, in change, allowRepeatedFieldNumber: true);
            }
        }

        writer.WriteBool(CoreCommandFieldNumbers.RelationshipCatchUp.HasMore, value.HasMore);
        if (value.NextCursor is { } nextCursor)
        {
            writer.WriteString(CoreCommandFieldNumbers.RelationshipCatchUp.NextCursor, nextCursor);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.RelationshipCatchUp.NextSequence, value.NextSequence);
        if (value.ResetRequired is { } resetRequired)
        {
            writer.WriteBool(CoreCommandFieldNumbers.RelationshipCatchUp.ResetRequired, resetRequired);
        }

        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.RelationshipCatchUp.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.RelationshipCatchUp.ErrorMessage, errorMessage);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(SyncBootstrapRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncBootstrapRequest.RequestId, nameof(SyncBootstrapRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncBootstrapRequest.ListLimit, nameof(SyncBootstrapRequest.ListLimit))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncBootstrapRequest.HistoryLimitPerConversation, nameof(SyncBootstrapRequest.HistoryLimitPerConversation))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncBootstrapRequest.MaxConversationsWithHistory, nameof(SyncBootstrapRequest.MaxConversationsWithHistory))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.SyncBootstrapRequest.Watermarks, nameof(SyncBootstrapRequest.Watermarks), typeof(ConversationSyncWatermarkSchema))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.SyncBootstrapRequest.RelationshipWatermarks, nameof(SyncBootstrapRequest.RelationshipWatermarks), typeof(RelationshipSyncWatermarkSchema))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncBootstrapRequest.RelationshipListLimit, nameof(SyncBootstrapRequest.RelationshipListLimit))]
public static partial class SyncBootstrapRequestSchema
{
    public static BinaryStatus TryEncode(
        in SyncBootstrapRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<SyncBootstrapRequestSchemaEncoder, SyncBootstrapRequest>(in value, destination, limits, out written);
}

public readonly struct SyncBootstrapRequestSchemaEncoder : IBinaryEncoder<SyncBootstrapRequestSchemaEncoder, SyncBootstrapRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in SyncBootstrapRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.SyncBootstrapRequest.RequestId, requestId);
        }

        writer.WriteInt32(CoreCommandFieldNumbers.SyncBootstrapRequest.ListLimit, value.ListLimit);
        writer.WriteInt32(CoreCommandFieldNumbers.SyncBootstrapRequest.HistoryLimitPerConversation, value.HistoryLimitPerConversation);
        writer.WriteInt32(CoreCommandFieldNumbers.SyncBootstrapRequest.MaxConversationsWithHistory, value.MaxConversationsWithHistory);
        if (value.Watermarks is { } watermarks)
        {
            foreach (ConversationSyncWatermark watermark in watermarks)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.SyncBootstrapRequest.Watermarks)) return writer.Status;
                writer.WriteNested<ConversationSyncWatermarkSchemaEncoder, ConversationSyncWatermark>(CoreCommandFieldNumbers.SyncBootstrapRequest.Watermarks, in watermark, allowRepeatedFieldNumber: true);
            }
        }

        if (value.RelationshipWatermarks is { } relationshipWatermarks)
        {
            foreach (RelationshipSyncWatermark relationshipWatermark in relationshipWatermarks)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.SyncBootstrapRequest.RelationshipWatermarks)) return writer.Status;
                writer.WriteNested<RelationshipSyncWatermarkSchemaEncoder, RelationshipSyncWatermark>(CoreCommandFieldNumbers.SyncBootstrapRequest.RelationshipWatermarks, in relationshipWatermark, allowRepeatedFieldNumber: true);
            }
        }

        if (value.RelationshipListLimit is { } relationshipListLimit)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.SyncBootstrapRequest.RelationshipListLimit, relationshipListLimit);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(SyncBootstrapResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncBootstrapResponse.RequestId, nameof(SyncBootstrapResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncBootstrapResponse.Succeeded, nameof(SyncBootstrapResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncBootstrapResponse.ErrorCode, nameof(SyncBootstrapResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncBootstrapResponse.ErrorMessage, nameof(SyncBootstrapResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncBootstrapResponse.ServerTimeMs, nameof(SyncBootstrapResponse.ServerTimeMs))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.SyncBootstrapResponse.Conversations, nameof(SyncBootstrapResponse.Conversations), typeof(TcpConversationListItemSchema))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.SyncBootstrapResponse.ConversationsNextCursor, nameof(SyncBootstrapResponse.ConversationsNextCursor), typeof(TcpConversationListCursorSchema))]
[TcpBinaryField(CoreCommandFieldNumbers.SyncBootstrapResponse.ConversationsHasMore, nameof(SyncBootstrapResponse.ConversationsHasMore))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.SyncBootstrapResponse.CatchUps, nameof(SyncBootstrapResponse.CatchUps), typeof(ConversationHistoryCatchUpSchema))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.SyncBootstrapResponse.ResetsRequired, nameof(SyncBootstrapResponse.ResetsRequired), typeof(SyncCursorResetRequiredSchema))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.SyncBootstrapResponse.RelationshipCatchUps, nameof(SyncBootstrapResponse.RelationshipCatchUps), typeof(RelationshipCatchUpSchema))]
public static partial class SyncBootstrapResponseSchema
{
    public static BinaryStatus TryEncode(
        in SyncBootstrapResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<SyncBootstrapResponseSchemaEncoder, SyncBootstrapResponse>(in value, destination, limits, out written);
}

public readonly struct SyncBootstrapResponseSchemaEncoder : IBinaryEncoder<SyncBootstrapResponseSchemaEncoder, SyncBootstrapResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in SyncBootstrapResponse value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.SyncBootstrapResponse.RequestId, requestId);
        }

        writer.WriteBool(CoreCommandFieldNumbers.SyncBootstrapResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.SyncBootstrapResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.SyncBootstrapResponse.ErrorMessage, errorMessage);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.SyncBootstrapResponse.ServerTimeMs, value.ServerTimeMs);
        if (value.Conversations is { } conversations)
        {
            foreach (TcpConversationListItem conversation in conversations)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.SyncBootstrapResponse.Conversations)) return writer.Status;
                writer.WriteNested<TcpConversationListItemSchemaEncoder, TcpConversationListItem>(CoreCommandFieldNumbers.SyncBootstrapResponse.Conversations, in conversation, allowRepeatedFieldNumber: true);
            }
        }

        if (value.ConversationsNextCursor is { } conversationsNextCursor)
        {
            writer.WriteNested<TcpConversationListCursorSchemaEncoder, TcpConversationListCursor>(CoreCommandFieldNumbers.SyncBootstrapResponse.ConversationsNextCursor, in conversationsNextCursor);
        }

        writer.WriteBool(CoreCommandFieldNumbers.SyncBootstrapResponse.ConversationsHasMore, value.ConversationsHasMore);
        if (value.CatchUps is { } catchUps)
        {
            foreach (ConversationHistoryCatchUp catchUp in catchUps)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.SyncBootstrapResponse.CatchUps)) return writer.Status;
                writer.WriteNested<ConversationHistoryCatchUpSchemaEncoder, ConversationHistoryCatchUp>(CoreCommandFieldNumbers.SyncBootstrapResponse.CatchUps, in catchUp, allowRepeatedFieldNumber: true);
            }
        }

        if (value.ResetsRequired is { } resetsRequired)
        {
            foreach (SyncCursorResetRequired resetsRequiredItem in resetsRequired)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.SyncBootstrapResponse.ResetsRequired)) return writer.Status;
                writer.WriteNested<SyncCursorResetRequiredSchemaEncoder, SyncCursorResetRequired>(CoreCommandFieldNumbers.SyncBootstrapResponse.ResetsRequired, in resetsRequiredItem, allowRepeatedFieldNumber: true);
            }
        }

        if (value.RelationshipCatchUps is { } relationshipCatchUps)
        {
            foreach (RelationshipCatchUp relationshipCatchUp in relationshipCatchUps)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.SyncBootstrapResponse.RelationshipCatchUps)) return writer.Status;
                writer.WriteNested<RelationshipCatchUpSchemaEncoder, RelationshipCatchUp>(CoreCommandFieldNumbers.SyncBootstrapResponse.RelationshipCatchUps, in relationshipCatchUp, allowRepeatedFieldNumber: true);
            }
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpRelationshipListItem))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListItem.UserId, nameof(TcpRelationshipListItem.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListItem.ResourceId, nameof(TcpRelationshipListItem.ResourceId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListItem.Status, nameof(TcpRelationshipListItem.Status))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListItem.Message, nameof(TcpRelationshipListItem.Message))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListItem.CreatedAtMs, nameof(TcpRelationshipListItem.CreatedAtMs))]
public static partial class TcpRelationshipListItemSchema
{
    public static BinaryStatus TryEncode(
        in TcpRelationshipListItem value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpRelationshipListItemSchemaEncoder, TcpRelationshipListItem>(in value, destination, limits, out written);
}

public readonly struct TcpRelationshipListItemSchemaEncoder : IBinaryEncoder<TcpRelationshipListItemSchemaEncoder, TcpRelationshipListItem>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpRelationshipListItem value)
    {
        writer.WriteInt64(CoreCommandFieldNumbers.TcpRelationshipListItem.UserId, value.UserId);
        writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListItem.ResourceId, value.ResourceId);
        if (value.Status is { } status)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListItem.Status, status);
        }

        if (value.Message is { } message)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListItem.Message, message);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.TcpRelationshipListItem.CreatedAtMs, value.CreatedAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpRelationshipListRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListRequest.RequestId, nameof(TcpRelationshipListRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListRequest.ListType, nameof(TcpRelationshipListRequest.ListType))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListRequest.PageSize, nameof(TcpRelationshipListRequest.PageSize))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListRequest.Cursor, nameof(TcpRelationshipListRequest.Cursor))]
public static partial class TcpRelationshipListRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpRelationshipListRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpRelationshipListRequestSchemaEncoder, TcpRelationshipListRequest>(in value, destination, limits, out written);
}

public readonly struct TcpRelationshipListRequestSchemaEncoder : IBinaryEncoder<TcpRelationshipListRequestSchemaEncoder, TcpRelationshipListRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpRelationshipListRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListRequest.RequestId, requestId);
        }

        writer.WriteUInt32(CoreCommandFieldNumbers.TcpRelationshipListRequest.ListType, (byte)value.ListType);
        if (value.PageSize is { } pageSize)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.TcpRelationshipListRequest.PageSize, pageSize);
        }

        if (value.Cursor is { } cursor)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListRequest.Cursor, cursor);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpRelationshipListResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListResponse.RequestId, nameof(TcpRelationshipListResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListResponse.ListType, nameof(TcpRelationshipListResponse.ListType))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListResponse.Succeeded, nameof(TcpRelationshipListResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListResponse.ErrorCode, nameof(TcpRelationshipListResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListResponse.ErrorMessage, nameof(TcpRelationshipListResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListResponse.ResetRequired, nameof(TcpRelationshipListResponse.ResetRequired))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.TcpRelationshipListResponse.Items, nameof(TcpRelationshipListResponse.Items), typeof(TcpRelationshipListItemSchema))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListResponse.NextCursor, nameof(TcpRelationshipListResponse.NextCursor))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListResponse.HasMore, nameof(TcpRelationshipListResponse.HasMore))]
public static partial class TcpRelationshipListResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpRelationshipListResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpRelationshipListResponseSchemaEncoder, TcpRelationshipListResponse>(in value, destination, limits, out written);
}

public readonly struct TcpRelationshipListResponseSchemaEncoder : IBinaryEncoder<TcpRelationshipListResponseSchemaEncoder, TcpRelationshipListResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpRelationshipListResponse value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListResponse.RequestId, requestId);
        }

        writer.WriteUInt32(CoreCommandFieldNumbers.TcpRelationshipListResponse.ListType, (byte)value.ListType);
        writer.WriteBool(CoreCommandFieldNumbers.TcpRelationshipListResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListResponse.ErrorMessage, errorMessage);
        }

        if (value.ResetRequired is { } resetRequired)
        {
            writer.WriteBool(CoreCommandFieldNumbers.TcpRelationshipListResponse.ResetRequired, resetRequired);
        }

        if (value.Items is { } items)
        {
            foreach (TcpRelationshipListItem item in items)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.TcpRelationshipListResponse.Items)) return writer.Status;
                writer.WriteNested<TcpRelationshipListItemSchemaEncoder, TcpRelationshipListItem>(CoreCommandFieldNumbers.TcpRelationshipListResponse.Items, in item, allowRepeatedFieldNumber: true);
            }
        }

        if (value.NextCursor is { } nextCursor)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListResponse.NextCursor, nextCursor);
        }

        writer.WriteBool(CoreCommandFieldNumbers.TcpRelationshipListResponse.HasMore, value.HasMore);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpRelationshipCommandRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipCommandRequest.RequestId, nameof(TcpRelationshipCommandRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipCommandRequest.Operation, nameof(TcpRelationshipCommandRequest.Operation))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipCommandRequest.TargetUserId, nameof(TcpRelationshipCommandRequest.TargetUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipCommandRequest.Message, nameof(TcpRelationshipCommandRequest.Message))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipCommandRequest.RequestIdToRespond, nameof(TcpRelationshipCommandRequest.RequestIdToRespond))]
public static partial class TcpRelationshipCommandRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpRelationshipCommandRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpRelationshipCommandRequestSchemaEncoder, TcpRelationshipCommandRequest>(in value, destination, limits, out written);
}

public readonly struct TcpRelationshipCommandRequestSchemaEncoder : IBinaryEncoder<TcpRelationshipCommandRequestSchemaEncoder, TcpRelationshipCommandRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpRelationshipCommandRequest value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipCommandRequest.RequestId, value.RequestId);
        writer.WriteUInt32(CoreCommandFieldNumbers.TcpRelationshipCommandRequest.Operation, (byte)value.Operation);
        if (value.TargetUserId is { } targetUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.TcpRelationshipCommandRequest.TargetUserId, targetUserId);
        }

        if (value.Message is { } message)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipCommandRequest.Message, message);
        }

        if (value.RequestIdToRespond is { } requestIdToRespond)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipCommandRequest.RequestIdToRespond, requestIdToRespond);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpRelationshipCommandResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.RequestId, nameof(TcpRelationshipCommandResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.Succeeded, nameof(TcpRelationshipCommandResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.ErrorCode, nameof(TcpRelationshipCommandResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.ErrorMessage, nameof(TcpRelationshipCommandResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.Operation, nameof(TcpRelationshipCommandResponse.Operation))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.TargetUserId, nameof(TcpRelationshipCommandResponse.TargetUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.ResourceId, nameof(TcpRelationshipCommandResponse.ResourceId))]
public static partial class TcpRelationshipCommandResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpRelationshipCommandResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpRelationshipCommandResponseSchemaEncoder, TcpRelationshipCommandResponse>(in value, destination, limits, out written);
}

public readonly struct TcpRelationshipCommandResponseSchemaEncoder : IBinaryEncoder<TcpRelationshipCommandResponseSchemaEncoder, TcpRelationshipCommandResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpRelationshipCommandResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.ErrorMessage, errorMessage);
        }

        if (value.Operation is { } operation)
        {
            writer.WriteUInt32(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.Operation, (byte)operation);
        }

        if (value.TargetUserId is { } targetUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.TargetUserId, targetUserId);
        }

        if (value.ResourceId is { } resourceId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipCommandResponse.ResourceId, resourceId);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpRelationshipListChangedUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListChangedUpdate.Resource, nameof(TcpRelationshipListChangedUpdate.Resource))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListChangedUpdate.Action, nameof(TcpRelationshipListChangedUpdate.Action))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListChangedUpdate.ResourceId, nameof(TcpRelationshipListChangedUpdate.ResourceId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListChangedUpdate.ActorUserId, nameof(TcpRelationshipListChangedUpdate.ActorUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListChangedUpdate.Message, nameof(TcpRelationshipListChangedUpdate.Message))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRelationshipListChangedUpdate.OccurredAtMs, nameof(TcpRelationshipListChangedUpdate.OccurredAtMs))]
public static partial class TcpRelationshipListChangedUpdateSchema
{
    public static BinaryStatus TryEncode(
        in TcpRelationshipListChangedUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpRelationshipListChangedUpdateSchemaEncoder, TcpRelationshipListChangedUpdate>(in value, destination, limits, out written);
}

public readonly struct TcpRelationshipListChangedUpdateSchemaEncoder : IBinaryEncoder<TcpRelationshipListChangedUpdateSchemaEncoder, TcpRelationshipListChangedUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpRelationshipListChangedUpdate value)
    {
        if (value.Resource is { } resource)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListChangedUpdate.Resource, resource);
        }

        if (value.Action is { } action)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListChangedUpdate.Action, action);
        }

        if (value.ResourceId is { } resourceId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListChangedUpdate.ResourceId, resourceId);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.TcpRelationshipListChangedUpdate.ActorUserId, value.ActorUserId);
        if (value.Message is { } message)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRelationshipListChangedUpdate.Message, message);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.TcpRelationshipListChangedUpdate.OccurredAtMs, value.OccurredAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpTypingNotify))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpTypingNotify.TargetUserId, nameof(TcpTypingNotify.TargetUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpTypingNotify.ConversationId, nameof(TcpTypingNotify.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpTypingNotify.IsTyping, nameof(TcpTypingNotify.IsTyping))]
public static partial class TcpTypingNotifySchema
{
    public static BinaryStatus TryEncode(
        in TcpTypingNotify value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpTypingNotifySchemaEncoder, TcpTypingNotify>(in value, destination, limits, out written);
}

public readonly struct TcpTypingNotifySchemaEncoder : IBinaryEncoder<TcpTypingNotifySchemaEncoder, TcpTypingNotify>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpTypingNotify value)
    {
        writer.WriteInt64(CoreCommandFieldNumbers.TcpTypingNotify.TargetUserId, value.TargetUserId);
        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpTypingNotify.ConversationId, conversationId);
        }

        writer.WriteBool(CoreCommandFieldNumbers.TcpTypingNotify.IsTyping, value.IsTyping);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpTypingUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpTypingUpdate.SenderUserId, nameof(TcpTypingUpdate.SenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpTypingUpdate.ConversationId, nameof(TcpTypingUpdate.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpTypingUpdate.IsTyping, nameof(TcpTypingUpdate.IsTyping))]
public static partial class TcpTypingUpdateSchema
{
    public static BinaryStatus TryEncode(
        in TcpTypingUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpTypingUpdateSchemaEncoder, TcpTypingUpdate>(in value, destination, limits, out written);
}

public readonly struct TcpTypingUpdateSchemaEncoder : IBinaryEncoder<TcpTypingUpdateSchemaEncoder, TcpTypingUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpTypingUpdate value)
    {
        writer.WriteInt64(CoreCommandFieldNumbers.TcpTypingUpdate.SenderUserId, value.SenderUserId);
        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpTypingUpdate.ConversationId, conversationId);
        }

        writer.WriteBool(CoreCommandFieldNumbers.TcpTypingUpdate.IsTyping, value.IsTyping);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpPresenceQueryRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpPresenceQueryRequest.RequestId, nameof(TcpPresenceQueryRequest.RequestId))]
[TcpBinaryRepeatedField(CoreCommandFieldNumbers.TcpPresenceQueryRequest.UserIds, nameof(TcpPresenceQueryRequest.UserIds))]
public static partial class TcpPresenceQueryRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpPresenceQueryRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpPresenceQueryRequestSchemaEncoder, TcpPresenceQueryRequest>(in value, destination, limits, out written);
}

public readonly struct TcpPresenceQueryRequestSchemaEncoder : IBinaryEncoder<TcpPresenceQueryRequestSchemaEncoder, TcpPresenceQueryRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpPresenceQueryRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpPresenceQueryRequest.RequestId, requestId);
        }

        if (value.UserIds is { } userIds)
        {
            foreach (long userId in userIds)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.TcpPresenceQueryRequest.UserIds)) return writer.Status;
                writer.WriteRepeatedInt64(CoreCommandFieldNumbers.TcpPresenceQueryRequest.UserIds, userId);
            }
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpPresenceUnwatchRequest))]
[TcpBinaryRepeatedField(CoreCommandFieldNumbers.TcpPresenceUnwatchRequest.UserIds, nameof(TcpPresenceUnwatchRequest.UserIds))]
public static partial class TcpPresenceUnwatchRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpPresenceUnwatchRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpPresenceUnwatchRequestSchemaEncoder, TcpPresenceUnwatchRequest>(in value, destination, limits, out written);
}

public readonly struct TcpPresenceUnwatchRequestSchemaEncoder : IBinaryEncoder<TcpPresenceUnwatchRequestSchemaEncoder, TcpPresenceUnwatchRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpPresenceUnwatchRequest value)
    {
        if (value.UserIds is { } userIds)
        {
            foreach (long userId in userIds)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.TcpPresenceUnwatchRequest.UserIds)) return writer.Status;
                writer.WriteRepeatedInt64(CoreCommandFieldNumbers.TcpPresenceUnwatchRequest.UserIds, userId);
            }
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpPresenceSnapshotItem))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpPresenceSnapshotItem.UserId, nameof(TcpPresenceSnapshotItem.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpPresenceSnapshotItem.IsOnline, nameof(TcpPresenceSnapshotItem.IsOnline))]
public static partial class TcpPresenceSnapshotItemSchema
{
    public static BinaryStatus TryEncode(
        in TcpPresenceSnapshotItem value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpPresenceSnapshotItemSchemaEncoder, TcpPresenceSnapshotItem>(in value, destination, limits, out written);
}

public readonly struct TcpPresenceSnapshotItemSchemaEncoder : IBinaryEncoder<TcpPresenceSnapshotItemSchemaEncoder, TcpPresenceSnapshotItem>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpPresenceSnapshotItem value)
    {
        writer.WriteInt64(CoreCommandFieldNumbers.TcpPresenceSnapshotItem.UserId, value.UserId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpPresenceSnapshotItem.IsOnline, value.IsOnline);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpPresenceSnapshotResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpPresenceSnapshotResponse.RequestId, nameof(TcpPresenceSnapshotResponse.RequestId))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.TcpPresenceSnapshotResponse.Items, nameof(TcpPresenceSnapshotResponse.Items), typeof(TcpPresenceSnapshotItemSchema))]
public static partial class TcpPresenceSnapshotResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpPresenceSnapshotResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpPresenceSnapshotResponseSchemaEncoder, TcpPresenceSnapshotResponse>(in value, destination, limits, out written);
}

public readonly struct TcpPresenceSnapshotResponseSchemaEncoder : IBinaryEncoder<TcpPresenceSnapshotResponseSchemaEncoder, TcpPresenceSnapshotResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpPresenceSnapshotResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpPresenceSnapshotResponse.RequestId, value.RequestId);
        if (value.Items is { } items)
        {
            foreach (TcpPresenceSnapshotItem item in items)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.TcpPresenceSnapshotResponse.Items)) return writer.Status;
                writer.WriteNested<TcpPresenceSnapshotItemSchemaEncoder, TcpPresenceSnapshotItem>(CoreCommandFieldNumbers.TcpPresenceSnapshotResponse.Items, in item, allowRepeatedFieldNumber: true);
            }
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpPresenceChanged))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpPresenceChanged.UserId, nameof(TcpPresenceChanged.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpPresenceChanged.IsOnline, nameof(TcpPresenceChanged.IsOnline))]
public static partial class TcpPresenceChangedSchema
{
    public static BinaryStatus TryEncode(
        in TcpPresenceChanged value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpPresenceChangedSchemaEncoder, TcpPresenceChanged>(in value, destination, limits, out written);
}

public readonly struct TcpPresenceChangedSchemaEncoder : IBinaryEncoder<TcpPresenceChangedSchemaEncoder, TcpPresenceChanged>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpPresenceChanged value)
    {
        writer.WriteInt64(CoreCommandFieldNumbers.TcpPresenceChanged.UserId, value.UserId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpPresenceChanged.IsOnline, value.IsOnline);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpRegisterPushTokenRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRegisterPushTokenRequest.RequestId, nameof(TcpRegisterPushTokenRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRegisterPushTokenRequest.Platform, nameof(TcpRegisterPushTokenRequest.Platform))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRegisterPushTokenRequest.Token, nameof(TcpRegisterPushTokenRequest.Token))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRegisterPushTokenRequest.AppDeviceLabel, nameof(TcpRegisterPushTokenRequest.AppDeviceLabel))]
public static partial class TcpRegisterPushTokenRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpRegisterPushTokenRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpRegisterPushTokenRequestSchemaEncoder, TcpRegisterPushTokenRequest>(in value, destination, limits, out written);
}

public readonly struct TcpRegisterPushTokenRequestSchemaEncoder : IBinaryEncoder<TcpRegisterPushTokenRequestSchemaEncoder, TcpRegisterPushTokenRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpRegisterPushTokenRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRegisterPushTokenRequest.RequestId, requestId);
        }

        writer.WriteUInt32(CoreCommandFieldNumbers.TcpRegisterPushTokenRequest.Platform, (byte)value.Platform);
        writer.WriteString(CoreCommandFieldNumbers.TcpRegisterPushTokenRequest.Token, value.Token);
        if (value.AppDeviceLabel is { } appDeviceLabel)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRegisterPushTokenRequest.AppDeviceLabel, appDeviceLabel);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpRegisterPushTokenResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRegisterPushTokenResponse.RequestId, nameof(TcpRegisterPushTokenResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRegisterPushTokenResponse.Succeeded, nameof(TcpRegisterPushTokenResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRegisterPushTokenResponse.ErrorCode, nameof(TcpRegisterPushTokenResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRegisterPushTokenResponse.ErrorMessage, nameof(TcpRegisterPushTokenResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRegisterPushTokenResponse.ActiveTokenCount, nameof(TcpRegisterPushTokenResponse.ActiveTokenCount))]
public static partial class TcpRegisterPushTokenResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpRegisterPushTokenResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpRegisterPushTokenResponseSchemaEncoder, TcpRegisterPushTokenResponse>(in value, destination, limits, out written);
}

public readonly struct TcpRegisterPushTokenResponseSchemaEncoder : IBinaryEncoder<TcpRegisterPushTokenResponseSchemaEncoder, TcpRegisterPushTokenResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpRegisterPushTokenResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpRegisterPushTokenResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpRegisterPushTokenResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRegisterPushTokenResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRegisterPushTokenResponse.ErrorMessage, errorMessage);
        }

        writer.WriteInt32(CoreCommandFieldNumbers.TcpRegisterPushTokenResponse.ActiveTokenCount, value.ActiveTokenCount);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpUnregisterPushTokenRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpUnregisterPushTokenRequest.RequestId, nameof(TcpUnregisterPushTokenRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpUnregisterPushTokenRequest.Token, nameof(TcpUnregisterPushTokenRequest.Token))]
public static partial class TcpUnregisterPushTokenRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpUnregisterPushTokenRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpUnregisterPushTokenRequestSchemaEncoder, TcpUnregisterPushTokenRequest>(in value, destination, limits, out written);
}

public readonly struct TcpUnregisterPushTokenRequestSchemaEncoder : IBinaryEncoder<TcpUnregisterPushTokenRequestSchemaEncoder, TcpUnregisterPushTokenRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpUnregisterPushTokenRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpUnregisterPushTokenRequest.RequestId, requestId);
        }

        if (value.Token is { } token)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpUnregisterPushTokenRequest.Token, token);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpUnregisterPushTokenResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpUnregisterPushTokenResponse.RequestId, nameof(TcpUnregisterPushTokenResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpUnregisterPushTokenResponse.Succeeded, nameof(TcpUnregisterPushTokenResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpUnregisterPushTokenResponse.ErrorCode, nameof(TcpUnregisterPushTokenResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpUnregisterPushTokenResponse.ErrorMessage, nameof(TcpUnregisterPushTokenResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpUnregisterPushTokenResponse.ActiveTokenCount, nameof(TcpUnregisterPushTokenResponse.ActiveTokenCount))]
public static partial class TcpUnregisterPushTokenResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpUnregisterPushTokenResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpUnregisterPushTokenResponseSchemaEncoder, TcpUnregisterPushTokenResponse>(in value, destination, limits, out written);
}

public readonly struct TcpUnregisterPushTokenResponseSchemaEncoder : IBinaryEncoder<TcpUnregisterPushTokenResponseSchemaEncoder, TcpUnregisterPushTokenResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpUnregisterPushTokenResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpUnregisterPushTokenResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpUnregisterPushTokenResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpUnregisterPushTokenResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpUnregisterPushTokenResponse.ErrorMessage, errorMessage);
        }

        writer.WriteInt32(CoreCommandFieldNumbers.TcpUnregisterPushTokenResponse.ActiveTokenCount, value.ActiveTokenCount);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpConversationMemberItem))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationMemberItem.UserId, nameof(TcpConversationMemberItem.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationMemberItem.Role, nameof(TcpConversationMemberItem.Role))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationMemberItem.JoinedAtMs, nameof(TcpConversationMemberItem.JoinedAtMs))]
public static partial class TcpConversationMemberItemSchema
{
    public static BinaryStatus TryEncode(
        in TcpConversationMemberItem value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpConversationMemberItemSchemaEncoder, TcpConversationMemberItem>(in value, destination, limits, out written);
}

public readonly struct TcpConversationMemberItemSchemaEncoder : IBinaryEncoder<TcpConversationMemberItemSchemaEncoder, TcpConversationMemberItem>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpConversationMemberItem value)
    {
        writer.WriteInt64(CoreCommandFieldNumbers.TcpConversationMemberItem.UserId, value.UserId);
        writer.WriteUInt32(CoreCommandFieldNumbers.TcpConversationMemberItem.Role, (byte)value.Role);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpConversationMemberItem.JoinedAtMs, value.JoinedAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpCreateGroupRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCreateGroupRequest.RequestId, nameof(TcpCreateGroupRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCreateGroupRequest.Title, nameof(TcpCreateGroupRequest.Title))]
[TcpBinaryRepeatedField(CoreCommandFieldNumbers.TcpCreateGroupRequest.MemberUserIds, nameof(TcpCreateGroupRequest.MemberUserIds))]
public static partial class TcpCreateGroupRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpCreateGroupRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpCreateGroupRequestSchemaEncoder, TcpCreateGroupRequest>(in value, destination, limits, out written);
}

public readonly struct TcpCreateGroupRequestSchemaEncoder : IBinaryEncoder<TcpCreateGroupRequestSchemaEncoder, TcpCreateGroupRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpCreateGroupRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpCreateGroupRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.TcpCreateGroupRequest.Title, value.Title);
        if (value.MemberUserIds is { } memberUserIds)
        {
            foreach (long memberUserId in memberUserIds)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.TcpCreateGroupRequest.MemberUserIds)) return writer.Status;
                writer.WriteRepeatedInt64(CoreCommandFieldNumbers.TcpCreateGroupRequest.MemberUserIds, memberUserId);
            }
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpCreateGroupResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCreateGroupResponse.RequestId, nameof(TcpCreateGroupResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCreateGroupResponse.Succeeded, nameof(TcpCreateGroupResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCreateGroupResponse.ErrorCode, nameof(TcpCreateGroupResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCreateGroupResponse.ErrorMessage, nameof(TcpCreateGroupResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCreateGroupResponse.ConversationId, nameof(TcpCreateGroupResponse.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpCreateGroupResponse.Title, nameof(TcpCreateGroupResponse.Title))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.TcpCreateGroupResponse.Members, nameof(TcpCreateGroupResponse.Members), typeof(TcpConversationMemberItemSchema))]
public static partial class TcpCreateGroupResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpCreateGroupResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpCreateGroupResponseSchemaEncoder, TcpCreateGroupResponse>(in value, destination, limits, out written);
}

public readonly struct TcpCreateGroupResponseSchemaEncoder : IBinaryEncoder<TcpCreateGroupResponseSchemaEncoder, TcpCreateGroupResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpCreateGroupResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpCreateGroupResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpCreateGroupResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpCreateGroupResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpCreateGroupResponse.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpCreateGroupResponse.ConversationId, conversationId);
        }

        if (value.Title is { } title)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpCreateGroupResponse.Title, title);
        }

        if (value.Members is { } members)
        {
            foreach (TcpConversationMemberItem member in members)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.TcpCreateGroupResponse.Members)) return writer.Status;
                writer.WriteNested<TcpConversationMemberItemSchemaEncoder, TcpConversationMemberItem>(CoreCommandFieldNumbers.TcpCreateGroupResponse.Members, in member, allowRepeatedFieldNumber: true);
            }
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpAddGroupMembersRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAddGroupMembersRequest.RequestId, nameof(TcpAddGroupMembersRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAddGroupMembersRequest.ConversationId, nameof(TcpAddGroupMembersRequest.ConversationId))]
[TcpBinaryRepeatedField(CoreCommandFieldNumbers.TcpAddGroupMembersRequest.MemberUserIds, nameof(TcpAddGroupMembersRequest.MemberUserIds))]
public static partial class TcpAddGroupMembersRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpAddGroupMembersRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpAddGroupMembersRequestSchemaEncoder, TcpAddGroupMembersRequest>(in value, destination, limits, out written);
}

public readonly struct TcpAddGroupMembersRequestSchemaEncoder : IBinaryEncoder<TcpAddGroupMembersRequestSchemaEncoder, TcpAddGroupMembersRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpAddGroupMembersRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAddGroupMembersRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.TcpAddGroupMembersRequest.ConversationId, value.ConversationId);
        foreach (long memberUserId in value.MemberUserIds)
        {
            if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.TcpAddGroupMembersRequest.MemberUserIds)) return writer.Status;
            writer.WriteRepeatedInt64(CoreCommandFieldNumbers.TcpAddGroupMembersRequest.MemberUserIds, memberUserId);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpAddGroupMembersResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.RequestId, nameof(TcpAddGroupMembersResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.Succeeded, nameof(TcpAddGroupMembersResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.ErrorCode, nameof(TcpAddGroupMembersResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.ErrorMessage, nameof(TcpAddGroupMembersResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.ConversationId, nameof(TcpAddGroupMembersResponse.ConversationId))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.Members, nameof(TcpAddGroupMembersResponse.Members), typeof(TcpConversationMemberItemSchema))]
public static partial class TcpAddGroupMembersResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpAddGroupMembersResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpAddGroupMembersResponseSchemaEncoder, TcpAddGroupMembersResponse>(in value, destination, limits, out written);
}

public readonly struct TcpAddGroupMembersResponseSchemaEncoder : IBinaryEncoder<TcpAddGroupMembersResponseSchemaEncoder, TcpAddGroupMembersResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpAddGroupMembersResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.ConversationId, conversationId);
        }

        if (value.Members is { } members)
        {
            foreach (TcpConversationMemberItem member in members)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.Members)) return writer.Status;
                writer.WriteNested<TcpConversationMemberItemSchemaEncoder, TcpConversationMemberItem>(CoreCommandFieldNumbers.TcpAddGroupMembersResponse.Members, in member, allowRepeatedFieldNumber: true);
            }
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpRemoveGroupMemberRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRemoveGroupMemberRequest.RequestId, nameof(TcpRemoveGroupMemberRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRemoveGroupMemberRequest.ConversationId, nameof(TcpRemoveGroupMemberRequest.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRemoveGroupMemberRequest.TargetUserId, nameof(TcpRemoveGroupMemberRequest.TargetUserId))]
public static partial class TcpRemoveGroupMemberRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpRemoveGroupMemberRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpRemoveGroupMemberRequestSchemaEncoder, TcpRemoveGroupMemberRequest>(in value, destination, limits, out written);
}

public readonly struct TcpRemoveGroupMemberRequestSchemaEncoder : IBinaryEncoder<TcpRemoveGroupMemberRequestSchemaEncoder, TcpRemoveGroupMemberRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpRemoveGroupMemberRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRemoveGroupMemberRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.TcpRemoveGroupMemberRequest.ConversationId, value.ConversationId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpRemoveGroupMemberRequest.TargetUserId, value.TargetUserId);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpRemoveGroupMemberResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRemoveGroupMemberResponse.RequestId, nameof(TcpRemoveGroupMemberResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRemoveGroupMemberResponse.Succeeded, nameof(TcpRemoveGroupMemberResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRemoveGroupMemberResponse.ErrorCode, nameof(TcpRemoveGroupMemberResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRemoveGroupMemberResponse.ErrorMessage, nameof(TcpRemoveGroupMemberResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRemoveGroupMemberResponse.ConversationId, nameof(TcpRemoveGroupMemberResponse.ConversationId))]
public static partial class TcpRemoveGroupMemberResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpRemoveGroupMemberResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpRemoveGroupMemberResponseSchemaEncoder, TcpRemoveGroupMemberResponse>(in value, destination, limits, out written);
}

public readonly struct TcpRemoveGroupMemberResponseSchemaEncoder : IBinaryEncoder<TcpRemoveGroupMemberResponseSchemaEncoder, TcpRemoveGroupMemberResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpRemoveGroupMemberResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpRemoveGroupMemberResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpRemoveGroupMemberResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRemoveGroupMemberResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRemoveGroupMemberResponse.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpRemoveGroupMemberResponse.ConversationId, conversationId);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpLeaveGroupRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpLeaveGroupRequest.RequestId, nameof(TcpLeaveGroupRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpLeaveGroupRequest.ConversationId, nameof(TcpLeaveGroupRequest.ConversationId))]
public static partial class TcpLeaveGroupRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpLeaveGroupRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpLeaveGroupRequestSchemaEncoder, TcpLeaveGroupRequest>(in value, destination, limits, out written);
}

public readonly struct TcpLeaveGroupRequestSchemaEncoder : IBinaryEncoder<TcpLeaveGroupRequestSchemaEncoder, TcpLeaveGroupRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpLeaveGroupRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpLeaveGroupRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.TcpLeaveGroupRequest.ConversationId, value.ConversationId);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpLeaveGroupResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpLeaveGroupResponse.RequestId, nameof(TcpLeaveGroupResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpLeaveGroupResponse.Succeeded, nameof(TcpLeaveGroupResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpLeaveGroupResponse.ErrorCode, nameof(TcpLeaveGroupResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpLeaveGroupResponse.ErrorMessage, nameof(TcpLeaveGroupResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpLeaveGroupResponse.ConversationId, nameof(TcpLeaveGroupResponse.ConversationId))]
public static partial class TcpLeaveGroupResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpLeaveGroupResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpLeaveGroupResponseSchemaEncoder, TcpLeaveGroupResponse>(in value, destination, limits, out written);
}

public readonly struct TcpLeaveGroupResponseSchemaEncoder : IBinaryEncoder<TcpLeaveGroupResponseSchemaEncoder, TcpLeaveGroupResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpLeaveGroupResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpLeaveGroupResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpLeaveGroupResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpLeaveGroupResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpLeaveGroupResponse.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpLeaveGroupResponse.ConversationId, conversationId);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpDissolveGroupRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpDissolveGroupRequest.RequestId, nameof(TcpDissolveGroupRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpDissolveGroupRequest.ConversationId, nameof(TcpDissolveGroupRequest.ConversationId))]
public static partial class TcpDissolveGroupRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpDissolveGroupRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpDissolveGroupRequestSchemaEncoder, TcpDissolveGroupRequest>(in value, destination, limits, out written);
}

public readonly struct TcpDissolveGroupRequestSchemaEncoder : IBinaryEncoder<TcpDissolveGroupRequestSchemaEncoder, TcpDissolveGroupRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpDissolveGroupRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpDissolveGroupRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.TcpDissolveGroupRequest.ConversationId, value.ConversationId);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpDissolveGroupResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpDissolveGroupResponse.RequestId, nameof(TcpDissolveGroupResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpDissolveGroupResponse.Succeeded, nameof(TcpDissolveGroupResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpDissolveGroupResponse.ErrorCode, nameof(TcpDissolveGroupResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpDissolveGroupResponse.ErrorMessage, nameof(TcpDissolveGroupResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpDissolveGroupResponse.ConversationId, nameof(TcpDissolveGroupResponse.ConversationId))]
public static partial class TcpDissolveGroupResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpDissolveGroupResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpDissolveGroupResponseSchemaEncoder, TcpDissolveGroupResponse>(in value, destination, limits, out written);
}

public readonly struct TcpDissolveGroupResponseSchemaEncoder : IBinaryEncoder<TcpDissolveGroupResponseSchemaEncoder, TcpDissolveGroupResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpDissolveGroupResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpDissolveGroupResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpDissolveGroupResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpDissolveGroupResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpDissolveGroupResponse.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpDissolveGroupResponse.ConversationId, conversationId);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpChangeMemberRoleRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpChangeMemberRoleRequest.RequestId, nameof(TcpChangeMemberRoleRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpChangeMemberRoleRequest.ConversationId, nameof(TcpChangeMemberRoleRequest.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpChangeMemberRoleRequest.TargetUserId, nameof(TcpChangeMemberRoleRequest.TargetUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpChangeMemberRoleRequest.NewRole, nameof(TcpChangeMemberRoleRequest.NewRole))]
public static partial class TcpChangeMemberRoleRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpChangeMemberRoleRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpChangeMemberRoleRequestSchemaEncoder, TcpChangeMemberRoleRequest>(in value, destination, limits, out written);
}

public readonly struct TcpChangeMemberRoleRequestSchemaEncoder : IBinaryEncoder<TcpChangeMemberRoleRequestSchemaEncoder, TcpChangeMemberRoleRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpChangeMemberRoleRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpChangeMemberRoleRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.TcpChangeMemberRoleRequest.ConversationId, value.ConversationId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpChangeMemberRoleRequest.TargetUserId, value.TargetUserId);
        writer.WriteUInt32(CoreCommandFieldNumbers.TcpChangeMemberRoleRequest.NewRole, (byte)value.NewRole);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpChangeMemberRoleResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpChangeMemberRoleResponse.RequestId, nameof(TcpChangeMemberRoleResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpChangeMemberRoleResponse.Succeeded, nameof(TcpChangeMemberRoleResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpChangeMemberRoleResponse.ErrorCode, nameof(TcpChangeMemberRoleResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpChangeMemberRoleResponse.ErrorMessage, nameof(TcpChangeMemberRoleResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpChangeMemberRoleResponse.ConversationId, nameof(TcpChangeMemberRoleResponse.ConversationId))]
public static partial class TcpChangeMemberRoleResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpChangeMemberRoleResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpChangeMemberRoleResponseSchemaEncoder, TcpChangeMemberRoleResponse>(in value, destination, limits, out written);
}

public readonly struct TcpChangeMemberRoleResponseSchemaEncoder : IBinaryEncoder<TcpChangeMemberRoleResponseSchemaEncoder, TcpChangeMemberRoleResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpChangeMemberRoleResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpChangeMemberRoleResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpChangeMemberRoleResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpChangeMemberRoleResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpChangeMemberRoleResponse.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpChangeMemberRoleResponse.ConversationId, conversationId);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpListGroupMembersRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpListGroupMembersRequest.RequestId, nameof(TcpListGroupMembersRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpListGroupMembersRequest.ConversationId, nameof(TcpListGroupMembersRequest.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpListGroupMembersRequest.PageSize, nameof(TcpListGroupMembersRequest.PageSize))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpListGroupMembersRequest.Cursor, nameof(TcpListGroupMembersRequest.Cursor))]
public static partial class TcpListGroupMembersRequestSchema
{
    public static BinaryStatus TryEncode(
        in TcpListGroupMembersRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpListGroupMembersRequestSchemaEncoder, TcpListGroupMembersRequest>(in value, destination, limits, out written);
}

public readonly struct TcpListGroupMembersRequestSchemaEncoder : IBinaryEncoder<TcpListGroupMembersRequestSchemaEncoder, TcpListGroupMembersRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpListGroupMembersRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpListGroupMembersRequest.RequestId, requestId);
        }

        writer.WriteString(CoreCommandFieldNumbers.TcpListGroupMembersRequest.ConversationId, value.ConversationId);
        if (value.PageSize is { } pageSize)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.TcpListGroupMembersRequest.PageSize, pageSize);
        }

        if (value.Cursor is { } cursor)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpListGroupMembersRequest.Cursor, cursor);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpListGroupMembersResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpListGroupMembersResponse.RequestId, nameof(TcpListGroupMembersResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpListGroupMembersResponse.Succeeded, nameof(TcpListGroupMembersResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpListGroupMembersResponse.ErrorCode, nameof(TcpListGroupMembersResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpListGroupMembersResponse.ErrorMessage, nameof(TcpListGroupMembersResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpListGroupMembersResponse.ConversationId, nameof(TcpListGroupMembersResponse.ConversationId))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.TcpListGroupMembersResponse.Members, nameof(TcpListGroupMembersResponse.Members), typeof(TcpConversationMemberItemSchema))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpListGroupMembersResponse.NextCursor, nameof(TcpListGroupMembersResponse.NextCursor))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpListGroupMembersResponse.HasMore, nameof(TcpListGroupMembersResponse.HasMore))]
public static partial class TcpListGroupMembersResponseSchema
{
    public static BinaryStatus TryEncode(
        in TcpListGroupMembersResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpListGroupMembersResponseSchemaEncoder, TcpListGroupMembersResponse>(in value, destination, limits, out written);
}

public readonly struct TcpListGroupMembersResponseSchemaEncoder : IBinaryEncoder<TcpListGroupMembersResponseSchemaEncoder, TcpListGroupMembersResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpListGroupMembersResponse value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpListGroupMembersResponse.RequestId, value.RequestId);
        writer.WriteBool(CoreCommandFieldNumbers.TcpListGroupMembersResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpListGroupMembersResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpListGroupMembersResponse.ErrorMessage, errorMessage);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpListGroupMembersResponse.ConversationId, conversationId);
        }

        if (value.Members is { } members)
        {
            foreach (TcpConversationMemberItem member in members)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.TcpListGroupMembersResponse.Members)) return writer.Status;
                writer.WriteNested<TcpConversationMemberItemSchemaEncoder, TcpConversationMemberItem>(CoreCommandFieldNumbers.TcpListGroupMembersResponse.Members, in member, allowRepeatedFieldNumber: true);
            }
        }

        if (value.NextCursor is { } nextCursor)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpListGroupMembersResponse.NextCursor, nextCursor);
        }

        writer.WriteBool(CoreCommandFieldNumbers.TcpListGroupMembersResponse.HasMore, value.HasMore);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpMemberJoinedUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberJoinedUpdate.ConversationId, nameof(TcpMemberJoinedUpdate.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberJoinedUpdate.UserId, nameof(TcpMemberJoinedUpdate.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberJoinedUpdate.Role, nameof(TcpMemberJoinedUpdate.Role))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberJoinedUpdate.ActorUserId, nameof(TcpMemberJoinedUpdate.ActorUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberJoinedUpdate.Title, nameof(TcpMemberJoinedUpdate.Title))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberJoinedUpdate.OccurredAtMs, nameof(TcpMemberJoinedUpdate.OccurredAtMs))]
public static partial class TcpMemberJoinedUpdateSchema
{
    public static BinaryStatus TryEncode(
        in TcpMemberJoinedUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpMemberJoinedUpdateSchemaEncoder, TcpMemberJoinedUpdate>(in value, destination, limits, out written);
}

public readonly struct TcpMemberJoinedUpdateSchemaEncoder : IBinaryEncoder<TcpMemberJoinedUpdateSchemaEncoder, TcpMemberJoinedUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpMemberJoinedUpdate value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpMemberJoinedUpdate.ConversationId, value.ConversationId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpMemberJoinedUpdate.UserId, value.UserId);
        writer.WriteUInt32(CoreCommandFieldNumbers.TcpMemberJoinedUpdate.Role, (byte)value.Role);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpMemberJoinedUpdate.ActorUserId, value.ActorUserId);
        if (value.Title is { } title)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpMemberJoinedUpdate.Title, title);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.TcpMemberJoinedUpdate.OccurredAtMs, value.OccurredAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpMemberLeftUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberLeftUpdate.ConversationId, nameof(TcpMemberLeftUpdate.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberLeftUpdate.UserId, nameof(TcpMemberLeftUpdate.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberLeftUpdate.OccurredAtMs, nameof(TcpMemberLeftUpdate.OccurredAtMs))]
public static partial class TcpMemberLeftUpdateSchema
{
    public static BinaryStatus TryEncode(
        in TcpMemberLeftUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpMemberLeftUpdateSchemaEncoder, TcpMemberLeftUpdate>(in value, destination, limits, out written);
}

public readonly struct TcpMemberLeftUpdateSchemaEncoder : IBinaryEncoder<TcpMemberLeftUpdateSchemaEncoder, TcpMemberLeftUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpMemberLeftUpdate value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpMemberLeftUpdate.ConversationId, value.ConversationId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpMemberLeftUpdate.UserId, value.UserId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpMemberLeftUpdate.OccurredAtMs, value.OccurredAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpMemberRemovedUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberRemovedUpdate.ConversationId, nameof(TcpMemberRemovedUpdate.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberRemovedUpdate.UserId, nameof(TcpMemberRemovedUpdate.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberRemovedUpdate.ActorUserId, nameof(TcpMemberRemovedUpdate.ActorUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMemberRemovedUpdate.OccurredAtMs, nameof(TcpMemberRemovedUpdate.OccurredAtMs))]
public static partial class TcpMemberRemovedUpdateSchema
{
    public static BinaryStatus TryEncode(
        in TcpMemberRemovedUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpMemberRemovedUpdateSchemaEncoder, TcpMemberRemovedUpdate>(in value, destination, limits, out written);
}

public readonly struct TcpMemberRemovedUpdateSchemaEncoder : IBinaryEncoder<TcpMemberRemovedUpdateSchemaEncoder, TcpMemberRemovedUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpMemberRemovedUpdate value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpMemberRemovedUpdate.ConversationId, value.ConversationId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpMemberRemovedUpdate.UserId, value.UserId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpMemberRemovedUpdate.ActorUserId, value.ActorUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpMemberRemovedUpdate.OccurredAtMs, value.OccurredAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpRoleChangedUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRoleChangedUpdate.ConversationId, nameof(TcpRoleChangedUpdate.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRoleChangedUpdate.UserId, nameof(TcpRoleChangedUpdate.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRoleChangedUpdate.NewRole, nameof(TcpRoleChangedUpdate.NewRole))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRoleChangedUpdate.PreviousRole, nameof(TcpRoleChangedUpdate.PreviousRole))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRoleChangedUpdate.ActorUserId, nameof(TcpRoleChangedUpdate.ActorUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpRoleChangedUpdate.OccurredAtMs, nameof(TcpRoleChangedUpdate.OccurredAtMs))]
public static partial class TcpRoleChangedUpdateSchema
{
    public static BinaryStatus TryEncode(
        in TcpRoleChangedUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpRoleChangedUpdateSchemaEncoder, TcpRoleChangedUpdate>(in value, destination, limits, out written);
}

public readonly struct TcpRoleChangedUpdateSchemaEncoder : IBinaryEncoder<TcpRoleChangedUpdateSchemaEncoder, TcpRoleChangedUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpRoleChangedUpdate value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpRoleChangedUpdate.ConversationId, value.ConversationId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpRoleChangedUpdate.UserId, value.UserId);
        writer.WriteUInt32(CoreCommandFieldNumbers.TcpRoleChangedUpdate.NewRole, (byte)value.NewRole);
        if (value.PreviousRole is { } previousRole)
        {
            writer.WriteUInt32(CoreCommandFieldNumbers.TcpRoleChangedUpdate.PreviousRole, (byte)previousRole);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.TcpRoleChangedUpdate.ActorUserId, value.ActorUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpRoleChangedUpdate.OccurredAtMs, value.OccurredAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpMembersAddedUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMembersAddedUpdate.ConversationId, nameof(TcpMembersAddedUpdate.ConversationId))]
[TcpBinaryRepeatedField(CoreCommandFieldNumbers.TcpMembersAddedUpdate.AddedUserIds, nameof(TcpMembersAddedUpdate.AddedUserIds))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMembersAddedUpdate.ActorUserId, nameof(TcpMembersAddedUpdate.ActorUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMembersAddedUpdate.Title, nameof(TcpMembersAddedUpdate.Title))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpMembersAddedUpdate.OccurredAtMs, nameof(TcpMembersAddedUpdate.OccurredAtMs))]
public static partial class TcpMembersAddedUpdateSchema
{
    public static BinaryStatus TryEncode(
        in TcpMembersAddedUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpMembersAddedUpdateSchemaEncoder, TcpMembersAddedUpdate>(in value, destination, limits, out written);
}

public readonly struct TcpMembersAddedUpdateSchemaEncoder : IBinaryEncoder<TcpMembersAddedUpdateSchemaEncoder, TcpMembersAddedUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpMembersAddedUpdate value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpMembersAddedUpdate.ConversationId, value.ConversationId);
        foreach (long addedUserId in value.AddedUserIds)
        {
            if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.TcpMembersAddedUpdate.AddedUserIds)) return writer.Status;
            writer.WriteRepeatedInt64(CoreCommandFieldNumbers.TcpMembersAddedUpdate.AddedUserIds, addedUserId);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.TcpMembersAddedUpdate.ActorUserId, value.ActorUserId);
        if (value.Title is { } title)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpMembersAddedUpdate.Title, title);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.TcpMembersAddedUpdate.OccurredAtMs, value.OccurredAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpConversationDissolvedUpdate))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationDissolvedUpdate.ConversationId, nameof(TcpConversationDissolvedUpdate.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationDissolvedUpdate.ActorUserId, nameof(TcpConversationDissolvedUpdate.ActorUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpConversationDissolvedUpdate.OccurredAtMs, nameof(TcpConversationDissolvedUpdate.OccurredAtMs))]
public static partial class TcpConversationDissolvedUpdateSchema
{
    public static BinaryStatus TryEncode(
        in TcpConversationDissolvedUpdate value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpConversationDissolvedUpdateSchemaEncoder, TcpConversationDissolvedUpdate>(in value, destination, limits, out written);
}

public readonly struct TcpConversationDissolvedUpdateSchemaEncoder : IBinaryEncoder<TcpConversationDissolvedUpdateSchemaEncoder, TcpConversationDissolvedUpdate>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpConversationDissolvedUpdate value)
    {
        writer.WriteString(CoreCommandFieldNumbers.TcpConversationDissolvedUpdate.ConversationId, value.ConversationId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpConversationDissolvedUpdate.ActorUserId, value.ActorUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpConversationDissolvedUpdate.OccurredAtMs, value.OccurredAtMs);
        return writer.Status;
    }
}