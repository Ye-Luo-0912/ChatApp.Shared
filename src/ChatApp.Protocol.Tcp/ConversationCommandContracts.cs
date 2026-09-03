namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// 会话列表 keyset 分页请求（C2S），由 <see cref="PacketCommand.ConversationListRequest"/> 承载。
/// </summary>
public sealed class ConversationListRequest
{
    public string? RequestId { get; set; }

    /// <summary>Before-cursor。null 表示从最前开始（置顶优先）。</summary>
    public bool? BeforeIsPinned { get; set; }

    public long? BeforePinnedAtMs { get; set; }

    public long? BeforeLastMessageAtMs { get; set; }

    public string? BeforeConversationId { get; set; }

    /// <summary>每页大小。0 或省略使用服务端默认。</summary>
    public int Limit { get; set; }
}

/// <summary>
/// 会话列表 keyset 分页结果（S2C），由 <see cref="PacketCommand.ConversationListPage"/> 承载。
/// </summary>
public sealed class ConversationListPage
{
    public string RequestId { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<TcpConversationListItem> Items { get; set; } = [];
    public TcpConversationListCursor? NextCursor { get; set; }
    public bool HasMore { get; set; }
}

/// <summary>
/// 将会话已读水位推进（C2S），由 <see cref="PacketCommand.ConversationMarkReadRequest"/> 承载。
/// </summary>
public sealed class ConversationMarkReadRequest
{
    public string? RequestId { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public long? ReadAtMs { get; set; }
    public string? ReadMessageId { get; set; }
}

/// <summary>
/// 会话已读水位推进结果（S2C），由 <see cref="PacketCommand.ConversationMarkReadResponse"/> 承载。
/// </summary>
public sealed class ConversationMarkReadResponse
{
    public string RequestId { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ConversationId { get; set; }
    public int UnreadCount { get; set; }
    public string? LastReadMessageId { get; set; }
    public long? LastReadAtMs { get; set; }
    public bool Changed { get; set; }
}

/// <summary>
/// 会话元数据变化事件（S2C），由 <see cref="PacketCommand.ConversationChanged"/> 承载。
/// </summary>
public sealed class ConversationChangedUpdate
{
    public string ConversationId { get; set; } = string.Empty;
    public TcpConversationType Type { get; set; }
    public long? PeerUserId { get; set; }
    public string? Title { get; set; }
    public string? LastMessageId { get; set; }
    public string? LastMessagePreview { get; set; }
    public long? LastMessageAtMs { get; set; }
    public long? LastSenderUserId { get; set; }
    public bool? IsPinned { get; set; }
    public bool? IsMuted { get; set; }
    public long? MutedUntilMs { get; set; }
}

/// <summary>
/// 未读计数变化事件（S2C），由 <see cref="PacketCommand.UnreadCountChanged"/> 承载。
/// </summary>
public sealed class UnreadCountChanged
{
    public string ConversationId { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    public string? LastReadMessageId { get; set; }
    public long? LastReadAtMs { get; set; }
}

/// <summary>
/// 会话偏好设置（置顶/免打扰）（C2S），由 <see cref="PacketCommand.ConversationSetPrefsRequest"/> 承载。
/// </summary>
public sealed class ConversationSetPrefsRequest
{
    public string? RequestId { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public bool? Pinned { get; set; }
    public bool? Muted { get; set; }
    public long? MutedUntilMs { get; set; }
}

/// <summary>
/// 会话偏好设置结果（S2C），由 <see cref="PacketCommand.ConversationSetPrefsResponse"/> 承载。
/// </summary>
public sealed class ConversationSetPrefsResponse
{
    public string RequestId { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ConversationId { get; set; }
    public bool IsPinned { get; set; }
    public bool IsMuted { get; set; }
    public long? MutedUntilMs { get; set; }
    public bool Changed { get; set; }
}

/// <summary>
/// 会话已读水位推进事件（S2C），由 <see cref="PacketCommand.ConversationRead"/> 承载。DM 与群聊共用。
/// </summary>
public sealed class ConversationReadUpdate
{
    public string ConversationId { get; set; } = string.Empty;
    public long ReaderUserId { get; set; }
    public string LastReadMessageId { get; set; } = string.Empty;
    public long LastReadAtMs { get; set; }
}

/// <summary>
/// 单条已读回执（何人、何时已读）。
/// </summary>
public sealed class MessageReadReceiptItem
{
    public long UserId { get; set; }
    public long ReadAtMs { get; set; }
}

/// <summary>
/// 群消息已读回执查询（C2S），由 <see cref="PacketCommand.MessageReadReceiptQueryRequest"/> 承载。
/// </summary>
public sealed class MessageReadReceiptQueryRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;

    /// <summary>分页游标（上一页最后一条已读者的 user_id）。null 表示第一页。</summary>
    public long? Cursor { get; set; }

    /// <summary>每页大小。0 或省略用服务端默认。</summary>
    public int PageSize { get; set; }
}

/// <summary>
/// 群消息已读回执查询结果（S2C），由 <see cref="PacketCommand.MessageReadReceiptQueryResponse"/> 承载。
/// </summary>
public sealed class MessageReadReceiptQueryResponse
{
    public string RequestId { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ConversationId { get; set; }
    public int ReadCount { get; set; }
    public int TotalMemberCount { get; set; }
    public bool IsSmallGroup { get; set; }
    public IReadOnlyList<MessageReadReceiptItem>? Readers { get; set; }
    public long? NextCursor { get; set; }
    public bool HasMore { get; set; }
}