namespace ChatApp.Shared.Protocol.Tcp;

public sealed class MessageReactionSummary
{
    public string Emoji { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool ReactedByMe { get; set; }
}

public sealed class MessageHistoryItem
{
    public string MessageId { get; set; } = string.Empty;
    public string ClientMessageId { get; set; } = string.Empty;
    public long SenderUserId { get; set; }
    public long ReceiverUserId { get; set; }
    public string? ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public long ReceivedAtMs { get; set; }
    public long? DeliveredAtMs { get; set; }
    public long? ReadAtMs { get; set; }
    public long? RecalledAtMs { get; set; }
    public int EditVersion { get; set; } = 1;
    public long? EditedAtMs { get; set; }
    public long ChangedAtMs { get; set; }
    public IReadOnlyList<TcpAttachmentRef>? Attachments { get; set; }
    public IReadOnlyList<MessageReactionSummary>? Reactions { get; set; }
    public string? ReplyToMessageId { get; set; }
    public long? ReplyToSenderUserId { get; set; }
    public string? ReplyToPreview { get; set; }
    public string? ForwardedFromMessageId { get; set; }
    public long? ForwardedFromSenderUserId { get; set; }
    public string? ForwardedFromPreview { get; set; }
    public IReadOnlyList<long>? MentionedUserIds { get; set; }
    public IReadOnlyList<string>? MentionedRoles { get; set; }
}

public sealed class MessageHistoryCursor
{
    /// <summary>Backward-history position, in Unix milliseconds.</summary>
    public long ReceivedAtMs { get; set; }

    /// <summary>
    /// Forward mutation position, in Unix milliseconds. Older peers omit it and
    /// consumers fall back to <see cref="ReceivedAtMs"/>.
    /// </summary>
    public long? ChangedAtMs { get; set; }

    public string MessageId { get; set; } = string.Empty;
}

public sealed class MessageHistoryRequest : ITcpRequest
{
    public string? RequestId { get; set; }
    public string? ConversationId { get; set; }
    public long? BeforeReceivedAtMs { get; set; }
    public string? BeforeMessageId { get; set; }

    /// <summary>
    /// The v1 field name is retained for compatibility; in forward mode the value
    /// is the message changed-at watermark.
    /// </summary>
    public long? AfterReceivedAtMs { get; set; }

    public string? AfterMessageId { get; set; }
    public int Limit { get; set; } = 50;
}

public sealed record MessageHistoryResponse
{
    public string? RequestId { get; set; }
    public string? ConversationId { get; set; }
    public bool Succeeded { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<MessageHistoryItem> Items { get; set; } = [];
    public MessageHistoryCursor? NextCursor { get; set; }
    public bool HasMore { get; set; }
}
