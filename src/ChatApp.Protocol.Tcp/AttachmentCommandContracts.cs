namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Attachment lifecycle change pushed down to the uploader (per-user notification),
/// carried by <see cref="PacketCommand.AttachmentLifecycleChanged"/>.
/// </summary>
public sealed class AttachmentLifecycleChanged
{
    public string AttachmentId { get; set; } = string.Empty;

    /// <summary>
    /// Lifecycle event status: 0=Scanning, 1=Available, 2=UploadConfirmed, 3=Rejected,
    /// 4=Expired, 5=ThumbnailUpdated. Deliberately wider than the binary attachment-reference
    /// availability status, so kept as <see cref="short"/> rather than a two-value enum.
    /// </summary>
    public short Status { get; set; }

    /// <summary>Status change time, Unix milliseconds.</summary>
    public long OccurredAtMs { get; set; }

    /// <summary>Rejection reason code (e.g. "virus", "policy") when <see cref="Status"/> is Rejected.</summary>
    public string? RejectReason { get; set; }

    /// <summary>Authenticated thumbnail download hint (non-public URL) when Available/ThumbnailUpdated.</summary>
    public string? ThumbnailApiHint { get; set; }

    /// <summary>New download token when Available/ThumbnailUpdated.</summary>
    public string? DownloadToken { get; set; }
}

/// <summary>
/// Attachment upload-completion confirmation request (C2S) carried by
/// <see cref="PacketCommand.AttachmentFinalizeRequest"/>. Triggered after chunk upload finishes.
/// </summary>
public sealed class AttachmentFinalizeRequest
{
    public string RequestId { get; set; } = string.Empty;

    /// <summary>Attachment id issued by the HTTP API at Initiate stage.</summary>
    public string AttachmentId { get; set; } = string.Empty;

    /// <summary>Upload size (bytes) for server-side completeness validation.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Optional content hash (SHA-256 hex) for server-side dedup validation.</summary>
    public string? ContentHash { get; set; }
}

/// <summary>
/// Attachment upload-completion confirmation response (S2C) carried by
/// <see cref="PacketCommand.AttachmentFinalizeResponse"/>.
/// </summary>
public sealed class AttachmentFinalizeResponse
{
    public string RequestId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>Attachment id.</summary>
    public string? AttachmentId { get; set; }

    /// <summary>Post-confirmation status (UploadConfirmed=2 or Rejected=3).</summary>
    public short? Status { get; set; }
}

/// <summary>
/// Attachment download-authorization request (C2S) carried by
/// <see cref="PacketCommand.AttachmentDownloadAuthorizeRequest"/>. Asks the server to issue a
/// short-lived signed download URL for a given attachment.
/// </summary>
public sealed class AttachmentDownloadAuthorizeRequest
{
    public string RequestId { get; set; } = string.Empty;

    /// <summary>Attachment id issued by the HTTP API at Initiate stage.</summary>
    public string AttachmentId { get; set; } = string.Empty;

    /// <summary>Optional conversation id to assist server-side permission checks.</summary>
    public string? ConversationId { get; set; }
}

/// <summary>
/// Attachment download-authorization response (S2C) carried by
/// <see cref="PacketCommand.AttachmentDownloadAuthorizeResponse"/>. Success carries a signed
/// short-lived download URL / token and its expiry; failure carries error code/message.
/// </summary>
public sealed class AttachmentDownloadAuthorizeResponse
{
    public string RequestId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>Attachment id.</summary>
    public string? AttachmentId { get; set; }

    /// <summary>Signed short-lived download URL (on success).</summary>
    public string? DownloadUrl { get; set; }

    /// <summary>Signature token (if the URL needs token auth).</summary>
    public string? DownloadToken { get; set; }

    /// <summary>Download URL expiry, Unix milliseconds.</summary>
    public long? ExpiresAtMs { get; set; }
}