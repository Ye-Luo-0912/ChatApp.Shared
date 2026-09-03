namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// 群组管理 wire 契约（P7）。
/// <para>
/// 覆盖 20 个群组控制面命令：创建/加人/移除/退出/解散/改角色/列成员 的
/// Request/Response 对，以及 <c>MemberJoined</c>/<c>MemberLeft</c>/<c>MemberRemoved</c>/
/// <c>RoleChanged</c>/<c>MembersAddedUpdate</c>/<c>ConversationDissolvedUpdate</c> 六个 S2C 更新事件。
/// 载荷形状镜像客户端 <c>Core.Models.DTO.GroupConversationDtos</c>。
/// </para>
/// </summary>
/// <remarks>
/// <para><b>成员角色</b>：<see cref="TcpGroupMemberRole"/>（Owner=1/Admin=2/Member=3），wire 为整数。</para>
/// <para><b>Request/Response</b>：请求携带可选 <c>RequestId</c>（响应原样回显），
/// 通用结果字段为 <c>Succeeded</c>/<c>ErrorCode</c>/<c>ErrorMessage</c>；成员列表用
/// <see cref="TcpConversationMemberItem"/>（<see cref="TcpBinaryField"/> 嵌套 repeated）。</para>
/// <para><b>S2C 更新</b>：无 C2S 对应，由服务端在当前在线连接上主动下发，
/// 无请求/响应配对；批量加人走聚合事件 <see cref="TcpMembersAddedUpdate"/>。</para>
/// </remarks>
public enum TcpGroupMemberRole : byte
{
    Owner = 1,
    Admin = 2,
    Member = 3
}

/// <summary>群成员条目（嵌套类型，用于响应与创建/加人结果）。</summary>
public sealed class TcpConversationMemberItem
{
    public long UserId { get; set; }

    public TcpGroupMemberRole Role { get; set; } = TcpGroupMemberRole.Member;

    public long JoinedAtMs { get; set; }
}

/// <summary>创建群聊请求（C2S），由 <see cref="PacketCommand.CreateGroupRequest"/> 承载。</summary>
public sealed class TcpCreateGroupRequest
{
    public string? RequestId { get; set; }

    public string Title { get; set; } = string.Empty;

    public IReadOnlyList<long>? MemberUserIds { get; set; }
}

/// <summary>创建群聊响应（S2C），由 <see cref="PacketCommand.CreateGroupResponse"/> 承载。</summary>
public sealed class TcpCreateGroupResponse
{
    public string RequestId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ConversationId { get; set; }

    public string? Title { get; set; }

    public IReadOnlyList<TcpConversationMemberItem>? Members { get; set; }
}

/// <summary>批量加入成员请求（C2S），由 <see cref="PacketCommand.AddGroupMembersRequest"/> 承载。</summary>
public sealed class TcpAddGroupMembersRequest
{
    public string? RequestId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public IReadOnlyList<long> MemberUserIds { get; set; } = [];
}

/// <summary>批量加入成员响应（S2C），由 <see cref="PacketCommand.AddGroupMembersResponse"/> 承载。</summary>
public sealed class TcpAddGroupMembersResponse
{
    public string RequestId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ConversationId { get; set; }

    public IReadOnlyList<TcpConversationMemberItem>? Members { get; set; }
}

/// <summary>移除成员请求（C2S），由 <see cref="PacketCommand.RemoveGroupMemberRequest"/> 承载。</summary>
public sealed class TcpRemoveGroupMemberRequest
{
    public string? RequestId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public long TargetUserId { get; set; }
}

/// <summary>移除成员响应（S2C），由 <see cref="PacketCommand.RemoveGroupMemberResponse"/> 承载。</summary>
public sealed class TcpRemoveGroupMemberResponse
{
    public string RequestId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ConversationId { get; set; }
}

/// <summary>主动退出群聊请求（C2S），由 <see cref="PacketCommand.LeaveGroupRequest"/> 承载。</summary>
public sealed class TcpLeaveGroupRequest
{
    public string? RequestId { get; set; }

    public string ConversationId { get; set; } = string.Empty;
}

/// <summary>主动退出群聊响应（S2C），由 <see cref="PacketCommand.LeaveGroupResponse"/> 承载。</summary>
public sealed class TcpLeaveGroupResponse
{
    public string RequestId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ConversationId { get; set; }
}

/// <summary>解散群聊请求（C2S），由 <see cref="PacketCommand.DissolveGroupRequest"/> 承载。</summary>
public sealed class TcpDissolveGroupRequest
{
    public string? RequestId { get; set; }

    public string ConversationId { get; set; } = string.Empty;
}

/// <summary>解散群聊响应（S2C），由 <see cref="PacketCommand.DissolveGroupResponse"/> 承载。</summary>
public sealed class TcpDissolveGroupResponse
{
    public string RequestId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ConversationId { get; set; }
}

/// <summary>变更成员角色请求（C2S），由 <see cref="PacketCommand.ChangeMemberRoleRequest"/> 承载。</summary>
public sealed class TcpChangeMemberRoleRequest
{
    public string? RequestId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public long TargetUserId { get; set; }

    public TcpGroupMemberRole NewRole { get; set; } = TcpGroupMemberRole.Member;
}

/// <summary>变更成员角色响应（S2C），由 <see cref="PacketCommand.ChangeMemberRoleResponse"/> 承载。</summary>
public sealed class TcpChangeMemberRoleResponse
{
    public string RequestId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ConversationId { get; set; }
}

/// <summary>列出群成员请求（C2S），由 <see cref="PacketCommand.ListGroupMembersRequest"/> 承载。</summary>
public sealed class TcpListGroupMembersRequest
{
    public string? RequestId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public int? PageSize { get; set; }

    public string? Cursor { get; set; }
}

/// <summary>列出群成员响应（S2C），由 <see cref="PacketCommand.ListGroupMembersResponse"/> 承载。</summary>
public sealed class TcpListGroupMembersResponse
{
    public string RequestId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ConversationId { get; set; }

    public IReadOnlyList<TcpConversationMemberItem>? Members { get; set; }

    public string? NextCursor { get; set; }

    public bool HasMore { get; set; }
}

/// <summary>
/// S2C 更新：成员加入群聊（<see cref="PacketCommand.MemberJoined"/>）、主动退出
/// （<see cref="PacketCommand.MemberLeft"/>）、被移出（<see cref="PacketCommand.MemberRemoved"/>）。
/// </summary>
public sealed class TcpMemberJoinedUpdate
{
    public string ConversationId { get; set; } = string.Empty;

    public long UserId { get; set; }

    public TcpGroupMemberRole Role { get; set; } = TcpGroupMemberRole.Member;

    public long ActorUserId { get; set; }

    public string? Title { get; set; }

    public long OccurredAtMs { get; set; }
}

/// <summary>S2C 更新：成员主动退出群聊。</summary>
public sealed class TcpMemberLeftUpdate
{
    public string ConversationId { get; set; } = string.Empty;

    public long UserId { get; set; }

    public long OccurredAtMs { get; set; }
}

/// <summary>S2C 更新：成员被移出群聊。</summary>
public sealed class TcpMemberRemovedUpdate
{
    public string ConversationId { get; set; } = string.Empty;

    public long UserId { get; set; }

    public long ActorUserId { get; set; }

    public long OccurredAtMs { get; set; }
}

/// <summary>S2C 更新：成员角色变更。</summary>
public sealed class TcpRoleChangedUpdate
{
    public string ConversationId { get; set; } = string.Empty;

    public long UserId { get; set; }

    public TcpGroupMemberRole NewRole { get; set; }

    public TcpGroupMemberRole? PreviousRole { get; set; }

    public long ActorUserId { get; set; }

    public long OccurredAtMs { get; set; }
}

/// <summary>S2C 更新：成员批量加入（聚合事件）。</summary>
public sealed class TcpMembersAddedUpdate
{
    public string ConversationId { get; set; } = string.Empty;

    public IReadOnlyList<long> AddedUserIds { get; set; } = [];

    public long ActorUserId { get; set; }

    public string? Title { get; set; }

    public long OccurredAtMs { get; set; }
}

/// <summary>S2C 更新：会话解散。</summary>
public sealed class TcpConversationDissolvedUpdate
{
    public string ConversationId { get; set; } = string.Empty;

    public long ActorUserId { get; set; }

    public long OccurredAtMs { get; set; }
}