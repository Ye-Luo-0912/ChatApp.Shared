namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Live chat message carried by <see cref="PacketCommand.ChatMessage"/>. Used both upstream
/// (client →server outbox) and downstream (server → recipient echo / push). Timestamps use
/// Unix milliseconds to keep the wire representation canonical and jitter-free.
/// </summary>
public sealed class ChatMessage
{
    /// <summary>Client-generated idempotency key. Downlink may echo it so the client can merge the reply with its local outbox.</summary>
    public string? ClientMessageId { get; set; }

    /// <summary>Server-assigned message id. Empty until the server assigns one.</summary>
    public string MessageId { get; set; } = string.Empty;

    public string? ConversationId { get; set; }

    /// <summary>The recipient of the message (absent for broadcast semantics such as group fan-out).</summary>
    public long TargetUserId { get; set; }

    public long SenderUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <summary>Client send time, Unix milliseconds.</summary>
    public long SentAtMs { get; set; }

    /// <summary>Upstream: confirmed attachment ids already persisted on the server.</summary>
    public IReadOnlyList<string>? AttachmentIds { get; set; }

    /// <summary>Downstream: attachment metadata (voice fields frozen via <see cref="TcpAttachmentRef"/>).</summary>
    public IReadOnlyList<TcpAttachmentRef>? Attachments { get; set; }

    public string? ReplyToMessageId { get; set; }
    public long? ReplyToSenderUserId { get; set; }
    public string? ReplyToPreview { get; set; }

    public string? ForwardedFromMessageId { get; set; }
    public long? ForwardedFromSenderUserId { get; set; }
    public string? ForwardedFromPreview { get; set; }

    /// <summary>User ids mentioned via '@' (group scenarios).</summary>
    public IReadOnlyList<long>? MentionedUserIds { get; set; }

    /// <summary>Mentioned role names (e.g. "all", "admin"); display only, no strict validation.</summary>
    public IReadOnlyList<string>? MentionedRoles { get; set; }
}

/// <summary>
/// Server-to-client acknowledgement for a delivered <see cref="PacketCommand.ChatMessage"/> frame.
/// </summary>
public sealed class MessageAcknowledgement
{
    /// <summary>Idempotency key echoed from the acknowledged message.</summary>
    public string? ClientMessageId { get; set; }

    public string? CommandId { get; set; }

    public bool Accepted { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>Server acknowledgement time, Unix milliseconds.</summary>
    public long AcknowledgedAtMs { get; set; }
}

/// <summary>
/// Read-receipt watermark. Downlink notifies that the peer read up to <see cref="LastReadMessageId"/>;
/// uplink informs the server that the current user has read up to that watermark.
/// </summary>
public sealed class MessageReceipt
{
    /// <summary>Request id carried on uplink to match <see cref="MessageReceiptAcknowledgement.RequestId"/>.</summary>
    public string? RequestId { get; set; }

    public string? ConversationId { get; set; }

    /// <summary>Watermark message id that has been read.</summary>
    public string? LastReadMessageId { get; set; }

    /// <summary>Read time, Unix milliseconds.</summary>
    public long? LastReadAtMs { get; set; }

    /// <summary>User who performed the read (usually the current user).</summary>
    public long? ReaderUserId { get; set; }

    /// <summary>The other side of the conversation.</summary>
    public long? ReceiverUserId { get; set; }
}

/// <summary>
/// Server acknowledgement of an upstream <see cref="PacketCommand.MessageReceipt"/>.
/// </summary>
public sealed class MessageReceiptAcknowledgement
{
    public string? RequestId { get; set; }

    public bool Accepted { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Server-pushed notification that a batch read-watermark changed (downlink only).
/// </summary>
public sealed class MessageReceiptUpdated
{
    public string? ConversationId { get; set; }

    /// <summary>New read-watermark message id.</summary>
    public string? LastReadMessageId { get; set; }

    /// <summary>New read time, Unix milliseconds.</summary>
    public long? LastReadAtMs { get; set; }

    /// <summary>The user whose read state advanced.</summary>
    public long? ReaderUserId { get; set; }
}