namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// 关系操作命令 / 列表变更通知 wire 契约（REL-E2E）。
/// <para>
/// 与 <see cref="TcpRelationshipListRequest"/>/<see cref="TcpRelationshipListResponse"/>
/// （只读列表分页）共同构成关系主链路：本契约承载 mutation 命令与下行变更通知，
/// 列表契约承载只读分页。mutation 永久走服务端权威，本契约开放命令能力位。
/// </para>
/// </summary>
/// <remarks>
/// <para><b>Operation</b>：见 <see cref="TcpRelationshipOperation"/>，数值与 Realtime <c>RelationshipOperation</c>
/// 一致（SendFriendRequest=1 … UnblockUser=6）。未知枚举值读取端按失败处理，不猜测操作。</para>
/// <para><b>资源键</b>：响应的 <c>ResourceId</c> 与列表变更的 <c>ResourceId</c> 是稳定资源键
/// （好友请求 Id / 友谊 Id / 黑名单记录 Id），客户端据此去重与应用，不依赖可变展示字段。</para>
/// <para><b>命令请求/响应</b>：请求必填 <c>RequestId</c> 与 <c>Operation</c>；<c>TargetUserId</c>
/// 用于 Send/Accept/Decline/Remove/Block/Unblock 指定对端；<c>Message</c> 仅 SendFriendRequest 附言；
/// <c>RequestIdToRespond</c> 仅 Respond 类操作（接受/拒绝指定请求）。响应回显
/// <c>RequestId</c>/<c>Operation</c>/<c>TargetUserId</c> 并提供 <c>Succeeded</c>/<c>ErrorCode</c>/<c>ResourceId</c>。</para>
/// <para><b>列表变更通知</b>：<c>Resource</c> ∈ "friend-request" / "friendship" / "blocked-user"；
/// <c>Action</c> 为请求状态（Pending/Accepted/Declined）或列表动作（changed/deleted/blocked/unblocked）；
/// <c>ActorUserId</c> 为触发变更的对端；<c>OccurredAtMs</c> 为事件时间（UTC 毫秒）。</para>
/// </remarks>
public static class TcpRelationshipCommandConstants
{
    /// <summary>RequestId 最大长度（UTF-8 字节）。</summary>
    public const int MaxRequestIdBytes = 64;

    /// <summary>Message（附言/错误说明）最大长度（UTF-8 字节）。</summary>
    public const int MaxMessageBytes = 512;

    /// <summary>ErrorCode / Resource / Action / ResourceId 最大长度（UTF-8 字节）。</summary>
    public const int MaxTokenBytes = 64;
}

/// <summary>
/// 关系操作类型。数值与 Realtime <c>RelationshipOperation</c> 一致。
/// </summary>
public enum TcpRelationshipOperation : byte
{
    SendFriendRequest = 1,
    AcceptFriendRequest = 2,
    DeclineFriendRequest = 3,
    RemoveFriend = 4,
    BlockUser = 5,
    UnblockUser = 6
}

/// <summary>
/// 关系操作命令请求（C2S），由 <see cref="PacketCommand.RelationshipCommandRequest"/> 承载。
/// </summary>
public sealed class TcpRelationshipCommandRequest
{
    public string RequestId { get; set; } = string.Empty;

    /// <summary>操作类型。</summary>
    public TcpRelationshipOperation Operation { get; set; }

    /// <summary>目标用户 Id（对方）。Send/Accept/Decline/Remove/Block/Unblock 均须指定。</summary>
    public long? TargetUserId { get; set; }

    /// <summary>好友请求附言（仅 SendFriendRequest 时使用）。</summary>
    public string? Message { get; set; }

    /// <summary>好友请求 Id（仅 Respond 操作：接受/拒绝指定请求）。</summary>
    public string? RequestIdToRespond { get; set; }
}

/// <summary>
/// 关系操作命令响应（S2C），由 <see cref="PacketCommand.RelationshipCommandResponse"/> 承载。
/// </summary>
public sealed class TcpRelationshipCommandResponse
{
    public string RequestId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    /// <summary>稳定错误码。成功时为 null。</summary>
    public string? ErrorCode { get; set; }

    /// <summary>面向用户的错误说明。</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>操作类型（回显）。</summary>
    public TcpRelationshipOperation? Operation { get; set; }

    /// <summary>目标用户 Id（回显）。</summary>
    public long? TargetUserId { get; set; }

    /// <summary>稳定资源键（好友请求 Id / 友谊 Id / 黑名单记录 Id）。</summary>
    public string? ResourceId { get; set; }
}

/// <summary>
/// 关系列表变更下行通知（S2C），由 <see cref="PacketCommand.RelationshipListChanged"/> 承载。
/// 服务端在好友请求、好友关系、拉黑列表变化时下发，提示客户端刷新对应列表。
/// </summary>
public sealed class TcpRelationshipListChangedUpdate
{
    /// <summary>资源类型：friend-request / friendship / blocked-user。</summary>
    public string? Resource { get; set; }

    /// <summary>动作语义（请求状态或列表动作）。</summary>
    public string? Action { get; set; }

    /// <summary>稳定资源键（请求 Id / 友谊 Id / 拉黑记录 Id）。</summary>
    public string? ResourceId { get; set; }

    /// <summary>触发变更的对端用户 Id。</summary>
    public long ActorUserId { get; set; }

    /// <summary>可选附言（好友请求消息）。</summary>
    public string? Message { get; set; }

    /// <summary>事件发生时间（UTC 毫秒）。</summary>
    public long OccurredAtMs { get; set; }
}