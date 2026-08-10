namespace ChatApp.Shared.Protocol.Tcp;

public enum TcpConversationType : byte
{
    Unknown = 0,
    Direct = 1,
    Group = 2
}

public sealed class TcpConversationListItem
{
    public string ConversationId { get; set; } = string.Empty;
    public TcpConversationType Type { get; set; } = TcpConversationType.Direct;
    public long? PeerUserId { get; set; }
    public string? Title { get; set; }
    public string? LastMessageId { get; set; }
    public string? LastMessagePreview { get; set; }
    public long? LastMessageAtMs { get; set; }
    public long? LastSenderUserId { get; set; }
    public int UnreadCount { get; set; }
    public string? LastReadMessageId { get; set; }
    public long? LastReadAtMs { get; set; }
    public bool IsPinned { get; set; }
    public long? PinnedAtMs { get; set; }
    public bool IsMuted { get; set; }
    public long? MutedUntilMs { get; set; }
}

public sealed class TcpConversationListCursor
{
    public bool IsPinned { get; set; }
    public long? PinnedAtMs { get; set; }
    public long? LastMessageAtMs { get; set; }
    public string ConversationId { get; set; } = string.Empty;
}

public sealed class ConversationSyncWatermark
{
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// The v1 field name is retained; the value is the last applied changed-at
    /// timestamp rather than the original receive timestamp.
    /// </summary>
    public long AfterReceivedAtMs { get; set; }

    public string AfterMessageId { get; set; } = string.Empty;
}

public enum TcpRelationshipListType : byte
{
    Friends = 1,
    FriendRequests = 2,
    BlockedUsers = 3
}

public enum TcpRelationshipChangeOperation : byte
{
    Upsert = 0,
    Delete = 1
}

public sealed class RelationshipSyncWatermark
{
    public TcpRelationshipListType ListType { get; set; }
    public long AfterSequence { get; set; }
}

public sealed class RelationshipChangeLogEntry
{
    public long ChangeSequence { get; set; }
    public TcpRelationshipChangeOperation Operation { get; set; }
    public string ResourceId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    public long CreatedAtMs { get; set; }
    public long OccurredAtMs { get; set; }
    public string? RequestId { get; set; }
}

public sealed class RelationshipCatchUp
{
    public TcpRelationshipListType ListType { get; set; }
    public IReadOnlyList<RelationshipChangeLogEntry> Changes { get; set; } = [];
    public bool HasMore { get; set; }
    public string? NextCursor { get; set; }
    public long NextSequence { get; set; }
    public long RetentionFloorSequence { get; set; }
    public bool ResetRequired { get; set; }
    public string? ResetReason { get; set; }
}

public enum TcpSyncCursorResetReason : byte
{
    MessageNotFound = 1,
    AheadOfTip = 2,
    MembershipLost = 3,
    GapTooLarge = 4,
    BeyondRetention = 5
}

public sealed class SyncCursorResetRequired
{
    public string ConversationId { get; set; } = string.Empty;
    public TcpSyncCursorResetReason Reason { get; set; }
    public string? TipMessageId { get; set; }

    /// <summary>v1 name; the value is the server tip changed-at timestamp.</summary>
    public long? TipReceivedAtMs { get; set; }

    /// <summary>v1 name; the value is the rejected client changed-at watermark.</summary>
    public long? ClientAfterReceivedAtMs { get; set; }

    public string? ClientAfterMessageId { get; set; }
}

public sealed class SyncBootstrapRequest : ITcpRequest
{
    public string? RequestId { get; set; }
    public int ListLimit { get; set; } = 50;
    public int HistoryLimitPerConversation { get; set; } = 20;
    public int MaxConversationsWithHistory { get; set; } = 10;
    public IReadOnlyList<ConversationSyncWatermark>? Watermarks { get; set; }
    public IReadOnlyList<RelationshipSyncWatermark>? RelationshipWatermarks { get; set; }
    public int? RelationshipListLimit { get; set; }
}

public sealed record ConversationHistoryCatchUp
{
    public string ConversationId { get; set; } = string.Empty;
    public IReadOnlyList<MessageHistoryItem> Items { get; set; } = [];
    public bool HasMore { get; set; }
    public MessageHistoryCursor? NextCursor { get; set; }
}

public sealed record SyncBootstrapResponse
{
    public string? RequestId { get; set; }
    public bool Succeeded { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public long ServerTimeMs { get; set; }
    public IReadOnlyList<TcpConversationListItem> Conversations { get; set; } = [];
    public TcpConversationListCursor? ConversationsNextCursor { get; set; }
    public bool ConversationsHasMore { get; set; }
    public IReadOnlyList<ConversationHistoryCatchUp> CatchUps { get; set; } = [];
    public IReadOnlyList<SyncCursorResetRequired> ResetsRequired { get; set; } = [];
    public IReadOnlyList<RelationshipCatchUp>? RelationshipCatchUps { get; set; }
}
