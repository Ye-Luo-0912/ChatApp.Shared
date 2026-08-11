namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// 关系只读列表 wire 契约（REL-WIRE-2）。
/// </summary>
/// <remarks>
/// <para><b>所有权边界。</b>本文件是 Client↔Gateway 之间关系只读列表的<b>唯一</b> wire schema。
/// Realtime 内部投影（<c>RelationshipProjectionDelta</c>、stream snapshot/checkpoint、
/// <c>RelationshipProjectionStreamDigest</c>、对账 hash）是内部契约，绝不进入外部 wire。</para>
///
/// <para><b>底层语义。</b>列表由 Server 权威写、Realtime 投影只读服务；Gateway 作为 producer 把
/// 可读结果映射为 TCP wire，Client 作为 consumer 直接消费本 schema。mutation 永久走 Server HTTP，
/// 本契约只开放读取能力位。</para>
/// </remarks>
///
/// <section>
/// <b>字段 / 预算 / 游标 / reset 语义表（v1）</b>
/// <list type="bullet">
///   <item><b>list type</b>：复用 <see cref="TcpRelationshipListType"/>（Friends=1 / FriendRequests=2 /
///   BlockedUsers=3），与 Realtime <c>RelationshipListType</c> 数值一致。未知枚举值：读取端保留原值并
///   按不可用处理（fail-closed），不猜测列表。</item>
///   <item><b>稳定资源键</b>：每一项的稳定键是 <see cref="TcpRelationshipListItem.ResourceId"/>
///   （好友请求 Id / 友谊 Id / 黑名单记录 Id）。分页、去重与排序必须以该键稳定展开，不得依赖
///   <c>Status/Message</c> 等可变展示字段。</item>
///   <item><b>version/watermark</b>：列表分页期间的版本一致性由 opaque cursor 承载；消费者不感知具体
///   编码。v1 不暴露持久化 version 字段，避免把 Realtime 内部版本泄漏给客户端。</item>
///   <item><b>页面方向</b>：仅向「后」分页（从第一页出发，逐页 <c>NextCursor</c> 前进）。无向前分页；
///   向前重读需从第一页（cursor=null）重新开始。</item>
///   <item><b>partial / 完整</b>：<c>Succeeded=true</c> 且 <c>HasMore=true</c> 表示本页为部分结果，
///   需用 <c>NextCursor</c> 续页；<c>HasMore=false</c> 表示已到尾页。</item>
///   <item><b>reset</b>：当分页期间版本变化（<c>relationship_projection_changed</c>）或检测到版本缺口
///   （<c>relationships_gap</c>）时，<c>Succeeded=false</c> 且 <c>ResetRequired=true</c>，消费者必须丢弃
///   本地游标并从第一页重建，不得用旧 cursor 续页。</item>
///   <item><b>unavailable / version-changed / gap</b>：稳定错误码见
///   <see cref="TcpRelationshipListErrorCode"/>。unavailable=投影尚未完成快照基线；version-changed=分页期间
///   列表变化需 reset；gap=检测到版本缺口需 reset。</item>
///   <item><b>null 与未知枚举</b>：<c>Status/Message</c> 为可空；未知 <c>Status</c> 字符串原样保留。
///   未知可选 JSON 字段读取端忽略，未知数值枚举字段保留原值。</item>
///   <item><b>分页预算</b>：PageSize 1–200，null/0 由生产者视为默认 50；单页 item 数上限 200。
///   <c>ResourceId</c> ≤ 64、<c>Status</c> ≤ 32、<c>Message</c> ≤ 512（字节，UTF-8）。</item>
///   <item><b>响应字节预算</b>：单响应 ≤ 80 KiB（UTF-8 字节）。超预算时生产者应以带
///   <c>request_too_large</c> 错误的失败响应终结，而不是静默截断。</item>
///   <item><b>opaque cursor</b>：<c>NextCursor</c> 只承诺「继续」或「明确失效」，不暴露 Realtime 内部编码。
///   畸形 / 无法识别的游标输入返回 <c>invalid_cursor</c>，不得静默从第一页重读。</item>
/// </list>
/// </section>
public static class TcpRelationshipListConstants
{
    /// <summary>PageSize 默认值（null 或 0 时由生产者采用）。</summary>
    public const int DefaultPageSize = 50;

    /// <summary>PageSize 允许的最小值。</summary>
    public const int MinPageSize = 1;

    /// <summary>PageSize 允许的最大值（也即单页 item 数上限）。</summary>
    public const int MaxPageSize = 200;

    /// <summary>ResourceId 最大长度（UTF-8 字节）。</summary>
    public const int MaxResourceIdBytes = 64;

    /// <summary>Status 最大长度（UTF-8 字节）。</summary>
    public const int MaxStatusBytes = 32;

    /// <summary>Message 最大长度（UTF-8 字节）。</summary>
    public const int MaxMessageBytes = 512;

    /// <summary>单响应最大字节数（UTF-8）。</summary>
    public const int MaxResponseBytes = 80 * 1024;
}

/// <summary>
/// 稳定的关系只读列表错误码。字符串值即 wire 值，生产端必须原样输出，消费端按表匹配。
/// </summary>
public static class TcpRelationshipListErrorCode
{
    /// <summary>投影尚未完成快照基线，暂不可用；消费者可稍后重试第一页。</summary>
    public const string ProjectionUnavailable = "relationship_read_projection_unavailable";

    /// <summary>分页期间列表版本变化，需 reset 并从第一页重建。</summary>
    public const string ProjectionChanged = "relationship_projection_changed";

    /// <summary>检测到版本缺口，需 reset 并从第一页重建。</summary>
    public const string GapDetected = "relationships_gap";

    /// <summary>分页游标畸形或无法识别，明确失效；消费者应丢弃游标从第一页重试。</summary>
    public const string InvalidCursor = "invalid_cursor";

    /// <summary>PageSize 超出 1–200；或响应字节预算溢出需以上报错误终结。</summary>
    public const string PageSizeOutOfRange = "page_size_out_of_range";

    /// <summary>请求响应超出单响应字节预算，拒绝而非截断。</summary>
    public const string RequestTooLarge = "request_too_large";

    /// <summary>请求 Id 缺失或列表类型不可用/未知。</summary>
    public const string BadRequest = "bad_request";
}

/// <summary>
/// 关系只读列表项（C2S 分页列表的稳定单元）。
/// </summary>
public sealed class TcpRelationshipListItem
{
    /// <summary>对方用户 Id。</summary>
    public long UserId { get; set; }

    /// <summary>稳定资源键（好友请求 Id / 友谊 Id / 黑名单记录 Id）。分页与去重以本字段展开。</summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>状态（Pending / Accepted / Blocked 等）。未知值原样保留。</summary>
    public string? Status { get; set; }

    /// <summary>好友请求附言（仅 FriendRequests 列表有此字段）。</summary>
    public string? Message { get; set; }

    /// <summary>关系建立时间（Unix 毫秒）。</summary>
    public long CreatedAtMs { get; set; }
}

/// <summary>
/// 关系只读列表查询请求（C2S）。
/// </summary>
public sealed class TcpRelationshipListRequest : ITcpRequest
{
    public string? RequestId { get; set; }

    /// <summary>列表类型。</summary>
    public TcpRelationshipListType ListType { get; set; }

    /// <summary>页大小（1–200）。null 或 0 表示默认值 50。</summary>
    public int? PageSize { get; set; }

    /// <summary>opaque 分页游标（null 表示第一页）。见语义表「opaque cursor」。</summary>
    public string? Cursor { get; set; }
}

/// <summary>
/// 关系只读列表响应（S2C）。
/// </summary>
public sealed class TcpRelationshipListResponse
{
    /// <summary>回显请求 RequestId。</summary>
    public string? RequestId { get; set; }

    /// <summary>回显请求列表类型。</summary>
    public TcpRelationshipListType ListType { get; set; }

    public bool Succeeded { get; set; }

    /// <summary>稳定错误码（见 <see cref="TcpRelationshipListErrorCode"/>）。成功时为 null。</summary>
    public string? ErrorCode { get; set; }

    /// <summary>面向用户的错误说明（不写日志正文）。</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// true 指示消费者必须丢弃本地游标并从第一页重建列表。可空：legacy 生产端无此字段时缺省为 null
    /// （语义等价于 false）。
    /// </summary>
    public bool? ResetRequired { get; set; }

    /// <summary>本页列表项。</summary>
    public IReadOnlyList<TcpRelationshipListItem> Items { get; set; } = [];

    /// <summary>续页 opaque 游标。HasMore=false 时为 null。</summary>
    public string? NextCursor { get; set; }

    /// <summary>true 表示本页为部分结果，需用 NextCursor 续页。</summary>
    public bool HasMore { get; set; }
}