namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// 关系增量同步（sync / catch-up）wire 契约（REL-WIRE-2）。
/// </summary>
/// <remarks>
/// <para><b>所有权边界。</b>本文件是 Client↔Gateway 之间关系列表<b>增量同步</b>的<b>唯一</b> wire schema。
/// Realtime 内部投影（<c>RelationshipProjectionDelta</c>、stream snapshot/checkpoint、原子导入、
/// 对账 hash、全局 change_sequence 与保留水位）是内部契约，绝不进入外部 wire。</para>
///
/// <para><b>与 list 只读 wire 的关系。</b>本契约做增量同步（返回变更日志 + 新水位）；全量重建由 list 只读
/// wire 承担（<see cref="TcpRelationshipListRequest"/>）。当本契约要求 reset 时，客户端丢弃本地该列表类型
/// 状态，改走全量 list 读重建，再续增量同步。</para>
///
/// <para><b>底层语义。</b>列表由 Server 权威写、Realtime 投影只读服务；Gateway 作为 producer 把可读增量映射
/// 为 TCP wire，Client 作为 consumer 直接消费本 schema。mutation 永久走 Server HTTP，本契约只开放读取能力位。</para>
/// </remarks>
///
/// <section>
/// <b>字段 / 预算 / 水位 / reset 语义表（v1）</b>
/// <list type="bullet">
///   <item><b>list type</b>：复用 <see cref="TcpRelationshipListType"/>（Friends=1 / FriendRequests=2 /
///   BlockedUsers=3），与 Realtime <c>RelationshipListType</c> 数值一致。未知枚举值：读取端保留原值并按
///   不可用处理（fail-closed），不猜测列表。</item>
///   <item><b>稳定资源键</b>：每个变更日志条目的稳定键是 <see cref="RelationshipChangeLogEntry.ResourceId"/>
///   （好友请求 Id / 友谊 Id / 黑名单记录 Id）。客户端按该键应用 Upsert/Delete，不依赖 <c>Status/Message</c>
///   等可变展示字段。</item>
///   <item><b>变更操作</b>：<see cref="TcpRelationshipChangeOperation"/>（Upsert=0 / Delete=1）。Upsert 按
///   ResourceId 写入/更新本地条目并应用其最新状态；Delete 按 ResourceId 移除本地条目（tombstone）。
///   未知操作值无法安全应用，读取端必须视为同步失败（fail-closed），不得跳过或猜测。</item>
///   <item><b>opaque 水位</b>：<see cref="RelationshipSyncWatermark.AfterSequence"/> 与
///   <see cref="RelationshipCatchUp.NextSequence"/> 是<b>不透明</b>的服务端定义位置令牌。客户端只持久化并
///   原样回传，绝不解释或自增；其编码变化不影响客户端。0 表示尚无任何增量（首次同步）。</item>
///   <item><b>分页</b>：仅向「后」同步。中间页 <c>HasMore=true</c> 用 <see cref="RelationshipCatchUp.NextCursor"/>
///   （opaque）续页，且<b>不得推进持久化水位</b>；<c>HasMore=false</c> 为尾页，客户端应用完该页全部变更后
///   才把 <see cref="RelationshipCatchUp.NextSequence"/> 持久化为新水位。畸形/无法识别的游标返回
///   <c>invalid_cursor</c>，不得静默从第一页重读。</item>
///   <item><b>reset</b>：当客户端水位低于服务端保留水位（<c>relationships_retention_exceeded</c>）、分页期间
///   版本变化（<c>relationship_projection_changed</c>）或检测到版本缺口（<c>relationships_gap</c>）时，
///   <c>ResetRequired=true</c> 并返回稳定 <see cref="RelationshipCatchUp.ErrorCode"/>。客户端必须丢弃本地该
///   列表类型状态，经全量 list 重建后重新建立水位，不得用旧水位续增量。</item>
///   <item><b>unavailable / 错误</b>：稳定错误码见 <see cref="TcpRelationshipSyncErrorCode"/>。unavailable=投影
///   尚未完成快照基线；invalid_cursor=游标畸形；bad_request=请求缺列表类型或水位畸形。</item>
///   <item><b>null 与未知枚举</b>：<c>Status/Message</c> 可空；未知 <c>Status</c> 字符串原样保留。未知可选
///   JSON 字段读取端忽略；未知数值枚举字段保留原值。</item>
///   <item><b>预算</b>：单变更条目 <c>ResourceId</c> ≤ 64、<c>Status</c> ≤ 32、<c>Message</c> ≤ 512（UTF-8 字节）；
///   单页变更条目数上限 200；单响应 ≤ 80 KiB（与 bootstrap 共享）。超预算时生产者应以 <c>request_too_large</c>
///   失败终结（本批由 bootstrap 顶层错误承载），而不是静默截断。</item>
/// </list>
/// </section>
public static class TcpRelationshipSyncConstants
{
    /// <summary>ResourceId 最大长度（UTF-8 字节）。</summary>
    public const int MaxResourceIdBytes = 64;

    /// <summary>Status 最大长度（UTF-8 字节）。</summary>
    public const int MaxStatusBytes = 32;

    /// <summary>Message 最大长度（UTF-8 字节）。</summary>
    public const int MaxMessageBytes = 512;

    /// <summary>单页变更条目数上限。</summary>
    public const int MaxChangesPerPage = 200;

    /// <summary>单响应最大字节数（UTF-8，与 bootstrap 共享）。</summary>
    public const int MaxResponseBytes = 80 * 1024;
}

/// <summary>
/// 关系增量同步稳定错误码。字符串值即 wire 值，生产端必须原样输出，消费端按表匹配。
/// </summary>
public static class TcpRelationshipSyncErrorCode
{
    /// <summary>投影尚未完成快照基线，暂不可用；消费者可稍后重试该列表类型的首轮同步。</summary>
    public const string ProjectionUnavailable = "relationship_read_projection_unavailable";

    /// <summary>分页期间列表版本变化，需 reset 并从全量 list 重建。</summary>
    public const string ProjectionChanged = "relationship_projection_changed";

    /// <summary>检测到版本缺口，需 reset 并从全量 list 重建。</summary>
    public const string GapDetected = "relationships_gap";

    /// <summary>分页游标畸形或无法识别，明确失效；消费者应丢弃游标从该列表类型首轮同步重试。</summary>
    public const string InvalidCursor = "invalid_cursor";

    /// <summary>客户端水位低于服务端保留水位，无法增量同步，需 reset 并从全量 list 重建。</summary>
    public const string RetentionExceeded = "relationships_retention_exceeded";

    /// <summary>响应超出单响应字节预算，拒绝而非截断。</summary>
    public const string RequestTooLarge = "request_too_large";

    /// <summary>请求缺列表类型或水位畸形。</summary>
    public const string BadRequest = "bad_request";
}

/// <summary>
/// 关系变更操作。增量同步按 <see cref="RelationshipChangeLogEntry.ResourceId"/> 应用。
/// </summary>
public enum TcpRelationshipChangeOperation : byte
{
    /// <summary>写入/更新本地条目并应用其最新状态。</summary>
    Upsert = 0,

    /// <summary>按 ResourceId 移除本地条目（tombstone）。</summary>
    Delete = 1
}

/// <summary>
/// 关系列表增量同步水位。客户端按 <see cref="ListType"/> 维度维护本地水位。
/// <para>
/// 水位语义：客户端已处理完所有 <see cref="RelationshipCatchUp.NextSequence"/> &lt;= 本水位的变更。
/// 下次同步时服务端返回序列高于本水位的增量变更。见语义表「opaque 水位」。
/// </para>
/// </summary>
public sealed class RelationshipSyncWatermark
{
    /// <summary>关系列表类型（Friends / FriendRequests / BlockedUsers）。</summary>
    public TcpRelationshipListType ListType { get; set; }

    /// <summary>opaque 水位令牌（见语义表「opaque 水位」）。0 表示首次同步。</summary>
    public long AfterSequence { get; set; }
}

/// <summary>
/// 关系变更日志条目：关系列表幂等增量同步的原子单元。
/// <para>
/// <see cref="Operation"/> = <see cref="TcpRelationshipChangeOperation.Upsert"/> 时客户端按
/// <see cref="ResourceId"/> upsert 本地条目（携带最新状态）；= Delete 时按 ResourceId 移除本地条目。
/// </para>
/// </summary>
public sealed class RelationshipChangeLogEntry
{
    /// <summary>变更操作（Upsert / Delete）。未知值必须视为同步失败（fail-closed）。</summary>
    public TcpRelationshipChangeOperation Operation { get; set; }

    /// <summary>稳定资源键（好友请求 Id / 友谊 Id / 黑名单记录 Id）。</summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>对方用户 Id（列表中的 peer）。</summary>
    public long UserId { get; set; }

    /// <summary>最新状态（Pending / Accepted / Blocked 等；仅 Upsert 有值）。</summary>
    public string? Status { get; set; }

    /// <summary>好友请求附言（仅 FriendRequests 列表）。</summary>
    public string? Message { get; set; }

    /// <summary>资源建立时间（Unix 毫秒）。</summary>
    public long CreatedAtMs { get; set; }

    /// <summary>变更发生时间（Unix 毫秒）。</summary>
    public long OccurredAtMs { get; set; }
}

/// <summary>
/// 关系列表增量同步结果：返回该列表类型从 opaque 水位起的变更日志 + 新水位。
/// <para>
/// 客户端按 <see cref="Changes"/> 应用本地状态；中间页用 <see cref="NextCursor"/> 续页且不推进水位，
/// 尾页（<c>HasMore=false</c>）应用全部变更后把 <see cref="NextSequence"/> 持久化为新水位。
/// <see cref="ResetRequired"/> 时丢弃本地该列表类型状态并经全量 list 重建。
/// </para>
/// </summary>
public sealed class RelationshipCatchUp
{
    /// <summary>关系列表类型。</summary>
    public TcpRelationshipListType ListType { get; set; }

    /// <summary>增量变更日志（Upsert / Delete 条目）。</summary>
    public IReadOnlyList<RelationshipChangeLogEntry> Changes { get; set; } = [];

    /// <summary>是否还有更多数据（分页）。</summary>
    public bool HasMore { get; set; }

    /// <summary>下一页 opaque 游标（见语义表「分页」）。null 表示无更多数据。</summary>
    public string? NextCursor { get; set; }

    /// <summary>尾页时应持久化作为下次同步水位的新令牌（见语义表「opaque 水位」）。</summary>
    public long NextSequence { get; set; }

    /// <summary>该列表类型是否需要客户端本地全量重建（水位超保留范围 / 版本变化 / 缺口）。可空以兼容旧生产端。</summary>
    public bool? ResetRequired { get; set; }

    /// <summary>稳定错误码（见 <see cref="TcpRelationshipSyncErrorCode"/>）。成功时为 null。</summary>
    public string? ErrorCode { get; set; }

    /// <summary>面向用户的错误说明（不写日志正文）。</summary>
    public string? ErrorMessage { get; set; }
}