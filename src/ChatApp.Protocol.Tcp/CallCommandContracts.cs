namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// 通话信令控制面 wire 契约（CALL-E2E-2）。
/// </summary>
/// <remarks>
/// <para><b>所有权边界。</b>本文件是 Client↔Gateway 之间通话<b>控制命令</b>的<b>唯一</b> wire schema。
/// Realtime 内部状态机（<c>CallStateMachine</c>）、临时状态保存（Redis/InMemory）、NATS subject、
/// <c>CallGrant</c> 签名细节与 signal 预算计数是内部契约，绝不进入外部 wire。</para>
///
/// <para><b>媒体边界。</b>本契约只承载控制命令与有界 SDP/ICE 信令载荷。音频媒体始终留在
/// WebRTC/STUN/TURN/SFU 媒体面；PostgreSQL、持久化 Outbox、聊天 JetStream 与 TCP Gateway 均不
/// 中转或保存音频媒体。SDP 仅随 <see cref="TcpCallCommandType.Invite/Accept/Reconnect"/> 出现，
/// 且只能经受预算限制的临时信令路径转发。</para>
///
/// <para><b>底层语义。</b>Server 签发短期 call grant 作为授权输入；Realtime 校验 grant 并驱动状态机；
/// Gateway 作为 producer 把上层命令映射为 TCP wire，Client 作为 consumer 直接消费本 schema。
/// 每个命令以 call id + command id 幂等、单调 revision 排序；未知命令、乱序、过期 grant 与终态后
/// 新命令均由稳定错误码承载（fail-closed），不猜测、不静默。</para>
/// </remarks>
///
/// <section>
/// <b>字段 / 预算 / 幂等 / 乱序语义表（v1）</b>
/// <list type="bullet">
///   <item><b>命令类型</b>：复用 <see cref="TcpCallCommandType"/>
///   （Invite=1 / Ringing=2 / Accept=3 / Reject=4 / Cancel=5 / End=6 / Reconnect=7），与 Realtime
///   <c>CallCommandType</c> 数值一致。未知枚举值：读取端按 <c>call_bad_request</c> 处理（fail-closed）。</item>
///   <item><b>幂等键</b>：<see cref="TcpCallCommandRequest.CommandId"/> 是同一 call 内命令的幂等键。
///   重复 command id 处理返回一致结果（<c>Replayed=true</c>），不产生新迁移。</item>
///   <item><b>乱序 / 过期</b>：<see cref="TcpCallCommandRequest.Revision"/> 单调，越大越新。非单调（乱序 /
///   过期）返回 <c>call_revision_stale</c>；终态（Ended）后收到新命令返回 <c>call_ended</c>。</item>
///   <item><b>grant 授权</b>：<see cref="TcpCallGrant"/> 是 Server 签发的短期授权输入。缺失、过期、签名/
///   参与方校验失败均 fail-closed（<c>call_grant_invalid</c> / <c>call_grant_expired</c>）。</item>
///   <item><b>状态 / 终态</b>：<see cref="TcpCallState"/>（Idle=0 / Ringing=1 / Active=2 / Ended=3）。
///   终态唯一：任何非 Ended 状态经合法迁移最终收敛到 Ended，一旦进入 Ended 不允许再迁出。</item>
///   <item><b>SDP 预算</b>：<see cref="TcpCallCommandRequest.Sdp"/> 与 <see cref="TcpCallSignal.Sdp"/>
///   ≤ 16 KiB（UTF-8 字节）。超预算返回 <c>call_sdp_invalid</c>，不截断。</item>
///   <item><b>响应字节预算</b>：单响应 ≤ 32 KiB（<see cref="TcpCallConstants.MaxResponseBytes"/>）。</item>
///   <item><b>stale 值</b>：同一 call 的乱序/重复命令返回 <c>bad_request</c> 之外的稳定错误，见
///   <see cref="TcpCallErrorCode"/>。</item>
/// </list>
/// </section>
public static class TcpCallConstants
{
    /// <summary>CallId 最大长度（UTF-8 字节）。</summary>
    public const int MaxCallIdBytes = 64;

    /// <summary>CommandId 最大长度（UTF-8 字节）。</summary>
    public const int MaxCommandIdBytes = 64;

    /// <summary>SDP/ICE 载荷最大长度（UTF-8 字节）。16 KiB 有界信令预算。</summary>
    public const int MaxSdpBytes = 16 * 1024;

    /// <summary>单响应最大字节数（UTF-8）。</summary>
    public const int MaxResponseBytes = 32 * 1024;
}

/// <summary>
/// 通话信令命令类型。C2S 命令以 call id + command id 幂等、单调 revision 排序。
/// 只有 Invite/Accept/Reconnect 可携带 SDP。
/// </summary>
public enum TcpCallCommandType : byte
{
    /// <summary>主叫发起呼叫（携带 SDP offer）。TcpCallState.Idle → Ringing。</summary>
    Invite = 1,

    /// <summary>被叫设备上报振铃（可选 ack，不携带 SDP）。状态不变。</summary>
    Ringing = 2,

    /// <summary>被叫接受呼叫（携带 SDP answer）。Ringing → Active。</summary>
    Accept = 3,

    /// <summary>被叫拒绝呼叫。Ringing → Ended（Rejected）。</summary>
    Reject = 4,

    /// <summary>主叫在接通前取消。Ringing → Ended（Cancelled）。</summary>
    Cancel = 5,

    /// <summary>任一方挂断。Ringing 或 Active → Ended（HungUp）。</summary>
    End = 6,

    /// <summary>断线后重连，重新建立临时 SDP/ICE 路由路径。状态不变。</summary>
    Reconnect = 7,
}

/// <summary>
/// 通话控制状态。终态唯一：任何非 <see cref="Ended"/> 状态经合法迁移最终收敛到
/// <see cref="Ended"/>，且一旦进入 <see cref="Ended"/> 不允许再迁出。
/// </summary>
public enum TcpCallState : byte
{
    /// <summary>无通话（仓储中不存在记录）。</summary>
    Idle = 0,

    /// <summary>振铃中：主叫已发起、被叫未应答。</summary>
    Ringing = 1,

    /// <summary>通话中：被叫已接受，SDP/ICE 已协商。</summary>
    Active = 2,

    /// <summary>终态：通话已结束（原因见 <see cref="TcpCallEndReason"/>）。</summary>
    Ended = 3,
}

/// <summary>
/// 通话终态原因。仅用于审计与客户端展示，不参与状态机迁移判定。
/// </summary>
public enum TcpCallEndReason : byte
{
    None = 0,
    Rejected = 1,
    Cancelled = 2,
    HungUp = 3,
    Missed = 4,
    TimedOut = 5,
}

/// <summary>
/// 通话信令稳定错误码。字符串值即 wire 值，生产端必须原样输出，消费端按表匹配。
/// </summary>
public static class TcpCallErrorCode
{
    /// <summary>命令编号缺失或超长。</summary>
    public const string InvalidCommandId = "call_invalid_command_id";

    /// <summary>call id 缺失或超长。</summary>
    public const string InvalidCallId = "call_invalid_call_id";

    /// <summary>通话参与者（主叫/被叫）缺失或无效。</summary>
    public const string InvalidParticipant = "call_invalid_participant";

    /// <summary>call grant 缺失、过期或签名/参与者校验失败（fail-closed）。</summary>
    public const string GrantInvalid = "call_grant_invalid";

    /// <summary>call grant 已过期。</summary>
    public const string GrantExpired = "call_grant_expired";

    /// <summary>SDP 载荷缺失或超过预算上限。</summary>
    public const string SdpInvalid = "call_sdp_invalid";

    /// <summary>该命令在当前状态下不允许（迁移表拒绝）。</summary>
    public const string InvalidTransition = "call_invalid_transition";

    /// <summary>命令 revision 非单调（乱序 / 过期），已拒绝。</summary>
    public const string RevisionStale = "call_revision_stale";

    /// <summary>通话不存在或已结束（终态后收到新命令）。</summary>
    public const string CallEnded = "call_ended";

    /// <summary>临时信令路径预算耗尽（信号条数超限）。</summary>
    public const string SignalBudgetExceeded = "call_signal_budget_exceeded";

    /// <summary>并发竞态导致 CAS 冲突，需重试。</summary>
    public const string ConflictRetry = "call_conflict_retry";

    /// <summary>临时状态仓储不可用（fail-closed）。</summary>
    public const string StateStoreUnavailable = "call_state_store_unavailable";

    /// <summary>请求缺 call id / command id / grant，或命令类型未知，或参与者不匹配。</summary>
    public const string BadRequest = "call_bad_request";
}

/// <summary>
/// Server 签发的短期 call grant，作为通话信令状态的授权输入（C2S 携带）。
/// <para>
/// 该 grant 是<em>不透明</em>授权凭证：Gateway/Client 不做签名校验，只原样携带；
/// 校验由 Realtime（<c>ICallGrantVerifier</c>）完成。媒体不共享此字段。
/// </para>
/// </summary>
public sealed class TcpCallGrant
{
    /// <summary>通话 Id。</summary>
    public string CallId { get; set; } = string.Empty;

    /// <summary>主叫用户 Id。</summary>
    public long CallerUserId { get; set; }

    /// <summary>被叫用户 Id。</summary>
    public long CalleeUserId { get; set; }

    /// <summary>grant 过期时间（Unix 毫秒）。短生命周期，授权输入有界。</summary>
    public long ExpiresAtMs { get; set; }

    /// <summary>一次性随机数，防重放。</summary>
    public string Nonce { get; set; } = string.Empty;

    /// <summary>不透明签名/指纹，由 Realtime 校验。</summary>
    public string? Signature { get; set; }
}

/// <summary>
/// 通话信令命令请求（C2S）。
/// </summary>
public sealed class TcpCallCommandRequest : ITcpRequest
{
    public string? RequestId { get; set; }

    /// <summary>幂等键：同一 call 内同一 command id 重复处理返回一致结果。</summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>通话 Id。</summary>
    public string CallId { get; set; } = string.Empty;

    /// <summary>命令类型。</summary>
    public TcpCallCommandType Type { get; set; }

    /// <summary>发起该命令的用户（主叫或被叫）。Gateway 会以可信身份校验。</summary>
    public long ActorUserId { get; set; }

    /// <summary>单调 revision，用于乱序/过期判定。越大越新。</summary>
    public long Revision { get; set; }

    /// <summary>Server 签发的短期 call grant（授权输入）。</summary>
    public TcpCallGrant? Grant { get; set; }

    /// <summary>可选 SDP（offer/answer）。仅 Invite/Accept/Reconnect 可携带，≤ 16 KiB。</summary>
    public string? Sdp { get; set; }

    /// <summary>客户端上报发生时间（仅诊断/展示）。</summary>
    public long ClientOccurredAtMs { get; set; }
}

/// <summary>
/// 通话信令载荷（SDP/ICE），只经受预算限制的临时信令路径转发给对端在线 Gateway（S2C push）。
/// <para>
/// 该载荷绝不进入 PostgreSQL、持久化 Outbox 或 JetStream 历史；音频媒体由 WebRTC/STUN/TURN/SFU
/// 承载。SDP ≤ 16 KiB。
/// </para>
/// </summary>
public sealed class TcpCallSignal
{
    /// <summary>信令唯一 Id（幂等去重键）。</summary>
    public string SignalId { get; set; } = string.Empty;

    /// <summary>通话 Id。</summary>
    public string CallId { get; set; } = string.Empty;

    /// <summary>对端信令来源用户 Id。</summary>
    public long FromUserId { get; set; }

    /// <summary>接收该信令的用户 Id。</summary>
    public long ToUserId { get; set; }

    /// <summary>信令类型（Invite/Accept/Reconnect 携带 SDP）。</summary>
    public TcpCallCommandType Kind { get; set; }

    /// <summary>SDP 载荷（≤ 16 KiB）。</summary>
    public string Sdp { get; set; } = string.Empty;

    /// <summary>对应的状态机 revision。</summary>
    public long Revision { get; set; }

    /// <summary>信令发生时间（Unix 毫秒）。</summary>
    public long OccurredAtMs { get; set; }
}

/// <summary>
/// 通话信令命令响应（S2C）。
/// </summary>
public sealed class TcpCallCommandResponse
{
    /// <summary>回显请求 RequestId。</summary>
    public string? RequestId { get; set; }

    /// <summary>回显通话 Id。</summary>
    public string CallId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    /// <summary>稳定错误码（见 <see cref="TcpCallErrorCode"/>）。成功时为 null。</summary>
    public string? ErrorCode { get; set; }

    /// <summary>面向用户的错误说明（不写日志正文）。</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>命令处理后的通话状态。</summary>
    public TcpCallState State { get; set; }

    /// <summary>终态原因（State=Ended 时有值）。</summary>
    public TcpCallEndReason EndReason { get; set; }

    /// <summary>命令处理后的状态机 revision。</summary>
    public long Revision { get; set; }

    /// <summary>是否因幂等重放返回（未发生新迁移）。</summary>
    public bool Replayed { get; set; }

    /// <summary>需要在临时信令路径转发给对端的 SDP（成功且携带 SDP 时）。</summary>
    public TcpCallSignal? SignalToForward { get; set; }
}