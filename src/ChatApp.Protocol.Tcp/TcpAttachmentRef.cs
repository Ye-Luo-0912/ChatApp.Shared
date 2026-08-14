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

    /// <summary>是否为语音附件（VOICE-MSG-2）。为 true 时语音编解码/容器/时长/采样率/声道必须非空且为正。</summary>
    public bool IsVoice { get; set; }

    /// <summary>音频编解码器（如 opus、aac）。仅语音附件有值。</summary>
    public string? VoiceCodec { get; set; }

    /// <summary>音频容器格式（如 ogg、m4a）。仅语音附件有值。</summary>
    public string? VoiceContainer { get; set; }

    /// <summary>语音时长（毫秒）。仅语音附件有值。</summary>
    public long? VoiceDurationMs { get; set; }

    /// <summary>采样率（Hz）。仅语音附件有值。</summary>
    public int? VoiceSampleRateHz { get; set; }

    /// <summary>声道数。仅语音附件有值。</summary>
    public short? VoiceChannels { get; set; }
}
