namespace ChatApp.Contracts.Http.Common;

using System.Text.Json.Serialization;

/// <summary>Realtime gateway endpoint returned by the HTTP authentication API.</summary>
public struct ServerEndpoint
{
    public string Host { get; set; }
    public string Name { get; set; }
    public ushort Port { get; set; }

    /// <summary>
    /// 传输 scheme，wire 名 <c>scheme</c>，语义与 <see cref="EndpointDescriptor.Scheme"/> 一一对应。
    /// null = 服务端未下发（旧 wire 形状）→ 消费者保持旧行为，不静默变严/变松；
    /// 未知数值保留在 wire 上但必须被 <see cref="EndpointPolicy"/> 拒绝（fail-closed）。
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EndpointScheme? Scheme { get; set; }

    /// <summary>
    /// TLS SNI / 目标主机覆盖，wire 名 <c>sniTargetHost</c>，语义与
    /// <see cref="EndpointDescriptor.SniTargetHost"/> 一一对应：null 或空白 = 回退 Host；
    /// 明文 scheme 上无意义、忽略不拒绝。
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SniTargetHost { get; set; }

    /// <summary>
    /// 最低 TLS 版本策略，wire 名 <c>minimumTls</c>，语义与 <see cref="EndpointDescriptor.MinimumTls"/>
    /// 一一对应。null = 未声明 → 消费者保持既有平台默认；未知数值必须被 <see cref="EndpointPolicy"/> 拒绝。
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MinimumTlsPolicy? MinimumTls { get; set; }
}

/// <summary>Stable cursor envelope used by HTTP collection endpoints.</summary>
public sealed class CursorPage<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }
}

/// <summary>The success envelope currently emitted by several HTTP mutation endpoints.</summary>
public sealed class ApiEnvelope<T>
{
    public T? Data { get; init; }
}
