namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// 输入状态 / 在线 / 推送注册 wire 契约（P6）。
/// <para>
/// 覆盖三组高频率、小载荷的实时控制面命令：输入状态（<c>TypingNotify</c>/<c>TypingUpdate</c>）、
/// 在线状态（<c>PresenceQuery</c>/<c>PresenceSnapshot</c>/<c>PresenceChanged</c>/<c>PresenceUnwatch</c>）、
/// 推送令牌注册（<c>RegisterPushTokenRequest/Response</c>/<c>UnregisterPushTokenRequest/Response</c>）。
/// </para>
/// </summary>
/// <remarks>
/// <para><b>输入状态</b>：C2S <see cref="TcpTypingNotify"/>，S2C <see cref="TcpTypingUpdate"/>。
/// <c>ConversationId</c> 可空（单聊或群聊统一维度），<c>IsTyping=true</c> 指示对方开始输入、
/// <c>false</c> 指示结束。瞬时语义，客户端可 DropOldest。</para>
/// <para><b>在线状态</b>：C2S <see cref="TcpPresenceQueryRequest"/>/<see cref="TcpPresenceUnwatchRequest"/>
/// 携带 <c>UserIds</c>（repeated int64）批量订阅/退订；S2C <see cref="TcpPresenceSnapshotResponse"/>
/// 返回快照（<see cref="TcpPresenceSnapshotItem"/> 列表），<see cref="TcpPresenceChanged"/> 推送在线变化。
/// <c>PresenceSnapshot</c> 回显 <c>RequestId</c> 便于请求匹配。</para>
/// <para><b>平台枚举</b>：<see cref="TcpPushPlatform"/>（Fcm=1/Apns=2/WebPush=3），wire 为整数。</para>
/// </remarks>
public static class TcpRealtimeStatusConstants
{
    /// <summary>RequestId 最大长度（UTF-8 字节）。</summary>
    public const int MaxRequestIdBytes = 64;

    /// <summary>推送令牌最大长度（UTF-8 字节），FCM ~150 / APNs 64 hex，留余量。</summary>
    public const int MaxTokenBytes = 1024;

    /// <summary>AppDeviceLabel 最大长度（UTF-8 字节）。</summary>
    public const int MaxAppDeviceLabelBytes = 128;
}

/// <summary>
/// 推送平台标识（wire 为整数）。数值与 Gateway <c>PushPlatform</c> 及客户端 <c>PushPlatformDto</c> 一致。
/// </summary>
public enum TcpPushPlatform : byte
{
    Fcm = 1,
    Apns = 2,
    WebPush = 3
}

/// <summary>
/// 输入状态通知（C2S），由 <see cref="PacketCommand.TypingNotify"/> 承载。
/// </summary>
public sealed class TcpTypingNotify
{
    /// <summary>目标用户 Id。</summary>
    public long TargetUserId { get; set; }

    /// <summary>会话 Id（可空）。</summary>
    public string? ConversationId { get; set; }

    /// <summary>true=开始输入，false=结束输入。</summary>
    public bool IsTyping { get; set; }
}

/// <summary>
/// 对端输入状态更新（S2C），由 <see cref="PacketCommand.TypingUpdate"/> 承载。
/// </summary>
public sealed class TcpTypingUpdate
{
    /// <summary>发送输入的源用户 Id。</summary>
    public long SenderUserId { get; set; }

    /// <summary>会话 Id（可空）。</summary>
    public string? ConversationId { get; set; }

    /// <summary>true=开始输入，false=结束输入。</summary>
    public bool IsTyping { get; set; }
}

/// <summary>
/// 在线状态查询请求（C2S），由 <see cref="PacketCommand.PresenceQuery"/> 承载。
/// </summary>
public sealed class TcpPresenceQueryRequest
{
    /// <summary>请求 Id（由发送方生成，服务端响应原样回显）。</summary>
    public string? RequestId { get; set; }

    /// <summary>要查询在线状态的目标用户 Id 列表。</summary>
    public IReadOnlyList<long>? UserIds { get; set; }
}

/// <summary>
/// 在线状态退订请求（C2S），由 <see cref="PacketCommand.PresenceUnwatch"/> 承载。
/// </summary>
public sealed class TcpPresenceUnwatchRequest
{
    /// <summary>要退订在线状态的目标用户 Id 列表。</summary>
    public IReadOnlyList<long>? UserIds { get; set; }
}

/// <summary>
/// 在线状态快照条目。
/// </summary>
public sealed class TcpPresenceSnapshotItem
{
    public long UserId { get; set; }

    public bool IsOnline { get; set; }
}

/// <summary>
/// 在线状态快照响应（S2C），由 <see cref="PacketCommand.PresenceSnapshot"/> 承载。
/// </summary>
public sealed class TcpPresenceSnapshotResponse
{
    public string RequestId { get; set; } = string.Empty;

    public IReadOnlyList<TcpPresenceSnapshotItem> Items { get; set; } = [];
}

/// <summary>
/// 在线状态变化推送（S2C），由 <see cref="PacketCommand.PresenceChanged"/> 承载。
/// </summary>
public sealed class TcpPresenceChanged
{
    public long UserId { get; set; }

    public bool IsOnline { get; set; }
}

/// <summary>
/// 注册推送令牌请求（C2S），由 <see cref="PacketCommand.RegisterPushTokenRequest"/> 承载。
/// 服务端按 (userId, deviceIdHash) 幂等覆盖；deviceIdHash 取自认证会话，忽略客户端传入。
/// </summary>
public sealed class TcpRegisterPushTokenRequest
{
    /// <summary>请求 Id（由发送方生成，服务端响应原样回显）。</summary>
    public string? RequestId { get; set; }

    /// <summary>推送平台（1=Fcm，2=Apns，3=WebPush）。</summary>
    public TcpPushPlatform Platform { get; set; }

    /// <summary>平台下发的推送令牌（长度上限 <see cref="TcpRealtimeStatusConstants.MaxTokenBytes"/>）。</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>可选应用级设备标识（多 App 共存去重）。</summary>
    public string? AppDeviceLabel { get; set; }
}

/// <summary>
/// 注册推送令牌响应（S2C），由 <see cref="PacketCommand.RegisterPushTokenResponse"/> 承载。
/// </summary>
public sealed class TcpRegisterPushTokenResponse
{
    public string RequestId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>当前用户已注册的推送令牌数（含本次）。</summary>
    public int ActiveTokenCount { get; set; }
}

/// <summary>
/// 注销推送令牌请求（C2S），由 <see cref="PacketCommand.UnregisterPushTokenRequest"/> 承载。
/// 不传 <c>Token</c> 时按当前连接 deviceIdHash 注销该设备全部令牌；传 <c>Token</c> 时按字符串精确注销。
/// </summary>
public sealed class TcpUnregisterPushTokenRequest
{
    /// <summary>请求 Id（由发送方生成，服务端响应原样回显）。</summary>
    public string? RequestId { get; set; }

    /// <summary>可选：精确指定要注销的令牌字符串。</summary>
    public string? Token { get; set; }
}

/// <summary>
/// 注销推送令牌响应（S2C），由 <see cref="PacketCommand.UnregisterPushTokenResponse"/> 承载。
/// </summary>
public sealed class TcpUnregisterPushTokenResponse
{
    public string RequestId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>注销后剩余的活跃推送令牌数。</summary>
    public int ActiveTokenCount { get; set; }
}