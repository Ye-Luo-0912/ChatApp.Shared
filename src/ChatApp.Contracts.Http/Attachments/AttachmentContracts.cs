using System.Text.Json.Serialization;

namespace ChatApp.Contracts.Http.Attachments;

public sealed class AttachmentPresignRequest
{
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public string? OriginalName { get; set; }
    public string? ClientAttachmentId { get; set; }

    /// <summary>
    /// File SHA-256 (64 lowercase hex chars). When present the server may answer with
    /// <see cref="AttachmentPresignResponse.Deduplicated"/> instead of issuing a real upload URL.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sha256 { get; set; }
}

public sealed class AttachmentPresignResponse
{
    public string AttachmentId { get; init; } = string.Empty;
    public string UploadUrl { get; init; } = string.Empty;
    public string DownloadPath { get; init; } = string.Empty;
    public string ObjectKey { get; init; } = string.Empty;
    public string Ticket { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// True when the server already holds content matching the requested SHA-256:
    /// the client must skip the upload and proceed directly to Confirm.
    /// </summary>
    public bool Deduplicated { get; init; }

    /// <summary>Headers that must be copied unchanged to the presigned PUT request.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? UploadHeaders { get; init; }

    [Obsolete("Use DownloadPath. Permanent PublicUrl is no longer returned.")]
    public string PublicUrl { get; init; } = string.Empty;
}

public sealed class ConfirmAttachmentRequest
{
    public string ObjectKey { get; set; } = string.Empty;
    public string? Ticket { get; set; }
    public string? AttachmentId { get; set; }
}

public sealed class ConfirmAttachmentResponse
{
    public long SagaId { get; init; }
    public string AttachmentId { get; init; } = string.Empty;
    public string DownloadPath { get; init; } = string.Empty;
    public string ObjectKey { get; init; } = string.Empty;
    public string Status { get; init; } = "Scanning";
    public string SagaStatus { get; init; } = "Requested";

    [Obsolete("Use DownloadPath. Permanent PublicUrl is no longer returned.")]
    public string PublicUrl { get; init; } = string.Empty;
}
