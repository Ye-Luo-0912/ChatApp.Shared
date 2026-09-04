using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Server 签发 / 消费端校验 <see cref="TcpCallGrant"/> 的 HMAC-SHA256 签名契约（群通话阶段一多人化）。
/// <para>
/// 规范载荷（canonical payload）以 <c>|</c> 分隔，Direct 与 0.5.6 之前的双人通话
/// <b>逐字节一致</b>（旧签名/旧校验零改动）：
/// <c>CallId|CallerUserId|CalleeUserId|ExpiresAtMs|Nonce</c>。
/// </para>
/// <para>
/// 群组（<see cref="TcpCallKind.Group"/>）载荷在双人载荷之后追加群组段，HMAC 因此覆盖
/// <b>全部参与者</b>（防替换/增删/重排攻击）：
/// <c>CallId|CallerUserId|CalleeUserId|ExpiresAtMs|Nonce|G|p1,p2,...,pn</c>，
/// 其中群组 grant 的 <c>CalleeUserId</c> 恒为 0，<c>p1..pn</c> 为升序参与者。
/// 群组段标记 <c>G</c> 进入签名输入，Direct/Group 载荷不可能互换重放；群组 grant 的
/// <c>CalleeUserId=0</c> 又使旧双人校验端（要求 Callee&gt;0）天然 fail-closed。
/// </para>
/// <para>
/// 密钥与 Server <c>JwtSettings.Secret</c> 同源；Realtime 生产校验端（1:1）与 Gateway 群组中继
/// 使用同一规范载荷与同一密钥。本类型只做纯计算（BCL-only），不做 I/O、不做时钟（<c>nowMs</c>
/// 由调用方传入）。
/// </para>
/// </summary>
public static class TcpCallGrantSignature
{
    /// <summary>规范载荷字段分隔符（字段均为受限字符集，不含该分隔符）。</summary>
    internal const char Separator = '|';

    /// <summary>群组段标记（进入签名输入，防 Direct/Group 载荷互换）。</summary>
    internal const char GroupMarker = 'G';

    /// <summary>群组参与者 Id 列表分隔符（十进制数字，无歧义）。</summary>
    internal const char ParticipantSeparator = ',';

    /// <summary>
    /// 构造规范载荷。Direct 与既有双人格式逐字节一致；Group 追加
    /// <c>|G|升序参与者列表</c>。
    /// </summary>
    public static string BuildCanonicalPayload(TcpCallGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);

        var payload = string.Concat(
            grant.CallId, Separator,
            grant.CallerUserId.ToString(CultureInfo.InvariantCulture), Separator,
            grant.CalleeUserId.ToString(CultureInfo.InvariantCulture), Separator,
            grant.ExpiresAtMs.ToString(CultureInfo.InvariantCulture), Separator,
            grant.Nonce);

        if (!IsGroup(grant))
            return payload;

        var participants = grant.Participants ?? [];
        var builder = new StringBuilder(payload.Length + 2 + participants.Count * 6);
        builder.Append(payload)
            .Append(Separator)
            .Append(GroupMarker)
            .Append(Separator);
        for (var i = 0; i < participants.Count; i++)
        {
            if (i > 0)
                builder.Append(ParticipantSeparator);
            builder.Append(participants[i].ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    /// <summary>计算 grant 的 HMAC-SHA256 签名（标准 Base64）。</summary>
    public static string Sign(string secret, TcpCallGrant grant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentNullException.ThrowIfNull(grant);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var digest = hmac.ComputeHash(Encoding.UTF8.GetBytes(BuildCanonicalPayload(grant)));
        return Convert.ToBase64String(digest);
    }

    /// <summary>
    /// 校验 grant（结构 + 过期 + 签名），fail-closed：任何不确定性都拒绝。
    /// </summary>
    /// <param name="grant">待校验 grant。</param>
    /// <param name="secret">共享签名密钥；未配置（null/空白）时拒绝。</param>
    /// <param name="nowMs">当前时间（Unix 毫秒），由调用方传入。</param>
    /// <param name="errorCode">失败时的稳定错误码（<see cref="TcpCallErrorCode.GrantInvalid"/> /
    /// <see cref="TcpCallErrorCode.GrantExpired"/>），成功为 null。</param>
    /// <param name="expiryGraceMs">过期判定宽限（毫秒），与 1:1 生产校验端一致（默认 500）。</param>
    /// <returns>签名与结构均有效且未过期时为 true。</returns>
    public static bool TryVerify(
        TcpCallGrant? grant,
        string? secret,
        long nowMs,
        out string? errorCode,
        long expiryGraceMs = 500)
    {
        errorCode = TcpCallErrorCode.GrantInvalid;
        if (grant is null || string.IsNullOrWhiteSpace(secret))
            return false;

        // ---- 结构（Direct 沿用双人规则；Group 用成员名单规则；未知种类 fail-closed）----
        if (string.IsNullOrWhiteSpace(grant.CallId)
            || grant.CallerUserId <= 0
            || grant.ExpiresAtMs <= 0
            || string.IsNullOrWhiteSpace(grant.Nonce))
        {
            return false;
        }

        var isGroup = IsGroup(grant);
        if (isGroup)
        {
            if (grant.CalleeUserId != 0 || !AreParticipantsCanonical(grant.Participants, grant.CallerUserId))
                return false;
        }
        else
        {
            if (grant.CallKind is not (null or TcpCallKind.Direct))
                return false;
            if (grant.CalleeUserId <= 0 || grant.CallerUserId == grant.CalleeUserId)
                return false;
            if (grant.Participants is { Count: > 0 })
                return false;
        }

        // ---- 过期 ----
        if (grant.ExpiresAtMs + expiryGraceMs < nowMs)
        {
            errorCode = TcpCallErrorCode.GrantExpired;
            return false;
        }

        // ---- 签名（缺失或比对失败均拒绝；恒定时间比较）----
        if (string.IsNullOrWhiteSpace(grant.Signature))
            return false;

        var expected = Sign(secret!, grant);
        if (!FixedTimeEquals(expected, grant.Signature!))
            return false;

        errorCode = null;
        return true;
    }

    /// <summary>是否群组 grant（<see cref="TcpCallKind.Group"/>）。未知种类按非群组处理，由结构校验拒绝。</summary>
    private static bool IsGroup(TcpCallGrant grant) => grant.CallKind == TcpCallKind.Group;

    /// <summary>
    /// 成员名单规范化校验：非 null、2..Max 人、含主叫、严格升序且无重复
    /// （签发端排序后签名，校验端只接受规范化形态，防止重排混淆）。
    /// </summary>
    private static bool AreParticipantsCanonical(IReadOnlyList<long>? participants, long callerUserId)
    {
        if (participants is null
            || participants.Count < 2
            || participants.Count > TcpCallConstants.MaxGroupCallParticipants)
        {
            return false;
        }

        var containsCaller = false;
        for (var i = 0; i < participants.Count; i++)
        {
            var current = participants[i];
            if (current <= 0)
                return false;
            if (current == callerUserId)
                containsCaller = true;
            if (i > 0 && current <= participants[i - 1])
                return false;
        }

        return containsCaller;
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
