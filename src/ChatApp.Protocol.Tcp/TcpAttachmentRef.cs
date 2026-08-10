namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>Stable attachment reference carried by TCP message payloads.</summary>
public sealed class TcpAttachmentRef
{
    public int RefVersion { get; set; } = 1;
    public string AttachmentId { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public short Status { get; set; }
    public string? DownloadApiHint { get; set; }
    public string? DownloadToken { get; set; }
    public string? ThumbnailApiHint { get; set; }
}
