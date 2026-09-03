namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Message edit command (C2S) carried by <see cref="PacketCommand.MessageEditRequest"/>.
/// </summary>
public sealed class MessageEditRequest
{
    public string? RequestId { get; set; }

    public string MessageId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Message edit acknowledgement (S2C) carried by <see cref="PacketCommand.MessageEditAck"/>.
/// </summary>
public sealed class MessageEditAcknowledgement
{
    public string RequestId { get; set; } = string.Empty;

    public string? MessageId { get; set; }

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ConversationId { get; set; }

    public string? Content { get; set; }

    public int? EditVersion { get; set; }

    public long? EditedAtMs { get; set; }
}

/// <summary>
/// Message edited event pushed to conversation members (S2C), carried by
/// <see cref="PacketCommand.MessageEdited"/>.
/// </summary>
public sealed class MessageEditedUpdate
{
    public string MessageId { get; set; } = string.Empty;

    public string? ConversationId { get; set; }

    public long SenderUserId { get; set; }

    public long ReceiverUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public int EditVersion { get; set; }

    public long EditedAtMs { get; set; }
}

/// <summary>
/// Message recall command (C2S) carried by <see cref="PacketCommand.MessageRecallRequest"/>.
/// </summary>
public sealed class MessageRecallRequest
{
    public string? RequestId { get; set; }

    public string MessageId { get; set; } = string.Empty;
}

/// <summary>
/// Message recall acknowledgement (S2C) carried by <see cref="PacketCommand.MessageRecallAck"/>.
/// </summary>
public sealed class MessageRecallAcknowledgement
{
    public string RequestId { get; set; } = string.Empty;

    public string? MessageId { get; set; }

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ConversationId { get; set; }

    public long? RecalledAtMs { get; set; }
}

/// <summary>
/// Message recalled event pushed to conversation members (S2C), carried by
/// <see cref="PacketCommand.MessageRecalled"/>.
/// </summary>
public sealed class MessageRecalledUpdate
{
    public string MessageId { get; set; } = string.Empty;

    public string? ConversationId { get; set; }

    public long SenderUserId { get; set; }

    public long ReceiverUserId { get; set; }

    public long RecalledAtMs { get; set; }
}

/// <summary>
/// Add-reaction command (C2S) carried by <see cref="PacketCommand.AddReactionRequest"/>.
/// </summary>
public sealed class AddReactionRequest
{
    public string? RequestId { get; set; }

    public string MessageId { get; set; } = string.Empty;

    public string Emoji { get; set; } = string.Empty;
}

/// <summary>
/// Add-reaction acknowledgement (S2C) carried by <see cref="PacketCommand.AddReactionAck"/>.
/// </summary>
public sealed class AddReactionAcknowledgement
{
    public string RequestId { get; set; } = string.Empty;

    public string? MessageId { get; set; }

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ConversationId { get; set; }

    public string? Emoji { get; set; }

    public long? OccurredAtMs { get; set; }

    public int? EmojiCount { get; set; }
}

/// <summary>
/// Reaction added event pushed to conversation members (S2C), carried by
/// <see cref="PacketCommand.ReactionAdded"/>.
/// </summary>
public sealed class ReactionAddedUpdate
{
    public string MessageId { get; set; } = string.Empty;

    public string? ConversationId { get; set; }

    public long ReactorUserId { get; set; }

    public long MessageSenderUserId { get; set; }

    public long MessageReceiverUserId { get; set; }

    public string Emoji { get; set; } = string.Empty;

    public int EmojiCount { get; set; }

    public long OccurredAtMs { get; set; }
}

/// <summary>
/// Remove-reaction command (C2S) carried by <see cref="PacketCommand.RemoveReactionRequest"/>.
/// </summary>
public sealed class RemoveReactionRequest
{
    public string? RequestId { get; set; }

    public string MessageId { get; set; } = string.Empty;

    public string Emoji { get; set; } = string.Empty;
}

/// <summary>
/// Remove-reaction acknowledgement (S2C) carried by <see cref="PacketCommand.RemoveReactionAck"/>.
/// </summary>
public sealed class RemoveReactionAcknowledgement
{
    public string RequestId { get; set; } = string.Empty;

    public string? MessageId { get; set; }

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ConversationId { get; set; }

    public string? Emoji { get; set; }

    public long? OccurredAtMs { get; set; }

    public int? EmojiCount { get; set; }
}

/// <summary>
/// Reaction removed event pushed to conversation members (S2C), carried by
/// <see cref="PacketCommand.ReactionRemoved"/>.
/// </summary>
public sealed class ReactionRemovedUpdate
{
    public string MessageId { get; set; } = string.Empty;

    public string? ConversationId { get; set; }

    public long ReactorUserId { get; set; }

    public long MessageSenderUserId { get; set; }

    public long MessageReceiverUserId { get; set; }

    public string Emoji { get; set; } = string.Empty;

    public int EmojiCount { get; set; }

    public long OccurredAtMs { get; set; }
}