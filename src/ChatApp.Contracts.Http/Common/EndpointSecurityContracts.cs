using System.Text.Json.Serialization;

namespace ChatApp.Contracts.Http.Common;

/// <summary>
/// Transport scheme of a service endpoint. Value 0 is reserved: a missing
/// <c>scheme</c> on the wire deserializes to 0 and must fail validation
/// instead of silently mapping to any concrete scheme.
/// </summary>
public enum EndpointScheme : byte
{
    /// <summary>Plaintext HTTP. Declared only for legacy or loopback deployments.</summary>
    Http = 1,

    /// <summary>HTTP over TLS.</summary>
    Https = 2,

    /// <summary>Plaintext raw TCP (e.g. a gateway listening without TLS).</summary>
    Tcp = 3,

    /// <summary>Raw TCP wrapped in TLS (the realtime gateway transport).</summary>
    TcpTls = 4
}

/// <summary>
/// Minimum TLS version policy. Values are ordered by strength; 0 explicitly means
/// "no minimum declared" — plaintext, or TLS terminated with the consumer's
/// platform default. It never tightens legacy behavior: consumers keep their
/// existing defaults when this value is absent.
/// </summary>
public enum MinimumTlsPolicy : byte
{
    None = 0,
    Tls12OrAbove = 1,
    Tls13Only = 2
}

/// <summary>
/// Versioned description of one service endpoint (HTTP API or TCP gateway) with
/// its minimum TLS policy. Additive-only wire evolution: old consumers skip
/// unknown fields, new consumers treat missing optional fields as "keep legacy
/// behavior". Certificate chain validation, pinning, revocation, dev exceptions
/// and platform TLS APIs are consumer-owned and intentionally absent here.
/// </summary>
public sealed class EndpointDescriptor
{
    public EndpointScheme Scheme { get; init; }

    /// <summary>DNS name, IPv4 or bracketed IPv6 literal. No path, port or userinfo.</summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>
    /// Explicit port. Null means "use the scheme default" (<see cref="EndpointPolicy.GetDefaultPort"/>);
    /// schemes without a default port (Tcp/TcpTls) must carry an explicit port to be valid.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? Port { get; init; }

    /// <summary>
    /// TLS SNI / target host override. Null or whitespace means "use <see cref="Host"/>".
    /// Meaningless on plaintext schemes and therefore ignored there, not rejected.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SniTargetHost { get; init; }

    public MinimumTlsPolicy MinimumTls { get; init; }
}

/// <summary>Single deterministic reason an <see cref="EndpointDescriptor"/> was rejected.</summary>
public enum EndpointPolicyViolation : byte
{
    None = 0,
    UnknownScheme = 1,
    UnknownTlsPolicy = 2,
    MissingHost = 3,
    HostTooLong = 4,
    HostInvalidCharacters = 5,
    SniHostInvalid = 6,
    MissingPort = 7,
    PlaintextSchemeWithTlsPolicy = 8
}

/// <summary>
/// Pure validation policy for <see cref="EndpointDescriptor"/>. Rules are checked in a
/// fixed order and the first violation wins, so the outcome of any input is deterministic
/// and testable. Unknown enum values fail closed: they deserialize with their numeric
/// value preserved (forward compatibility) but never validate as a safe default.
/// </summary>
public static class EndpointPolicy
{
    /// <summary>RFC 1035 max fully-qualified domain name length; also bounds IP literals.</summary>
    public const int MaxHostLength = 253;

    /// <summary>
    /// Validates an endpoint descriptor. Rejects unknown enum values, structurally
    /// invalid hosts, TCP schemes without an explicit port, and insecure combinations
    /// (a plaintext scheme that declares a TLS policy).
    /// </summary>
    public static bool TryValidate(EndpointDescriptor? endpoint, out EndpointPolicyViolation violation)
    {
        if (endpoint is null)
        {
            violation = EndpointPolicyViolation.MissingHost;
            return false;
        }

        if (!Enum.IsDefined(endpoint.Scheme))
        {
            violation = EndpointPolicyViolation.UnknownScheme;
            return false;
        }

        if (!Enum.IsDefined(endpoint.MinimumTls))
        {
            violation = EndpointPolicyViolation.UnknownTlsPolicy;
            return false;
        }

        if (string.IsNullOrWhiteSpace(endpoint.Host))
        {
            violation = EndpointPolicyViolation.MissingHost;
            return false;
        }

        if (endpoint.Host.Length > MaxHostLength)
        {
            violation = EndpointPolicyViolation.HostTooLong;
            return false;
        }

        if (!HasValidHostCharacters(endpoint.Host))
        {
            violation = EndpointPolicyViolation.HostInvalidCharacters;
            return false;
        }

        // 空白视为未覆盖（与消费者现有 TlsServerName 语义一致），非空白才按 host 规则校验。
        if (!string.IsNullOrWhiteSpace(endpoint.SniTargetHost)
            && (endpoint.SniTargetHost.Length > MaxHostLength
                || !HasValidHostCharacters(endpoint.SniTargetHost)))
        {
            violation = EndpointPolicyViolation.SniHostInvalid;
            return false;
        }

        if (endpoint.Port is null && GetDefaultPort(endpoint.Scheme) is null)
        {
            violation = EndpointPolicyViolation.MissingPort;
            return false;
        }

        if (IsPlaintext(endpoint.Scheme) && endpoint.MinimumTls is not MinimumTlsPolicy.None)
        {
            violation = EndpointPolicyViolation.PlaintextSchemeWithTlsPolicy;
            return false;
        }

        violation = EndpointPolicyViolation.None;
        return true;
    }

    /// <summary>
    /// True for schemes that carry no TLS by definition (<see cref="EndpointScheme.Http"/>,
    /// <see cref="EndpointScheme.Tcp"/>). Unknown values return false; they are rejected
    /// upstream by the unknown-value rule.
    /// </summary>
    public static bool IsPlaintext(EndpointScheme scheme) => scheme is EndpointScheme.Http or EndpointScheme.Tcp;

    /// <summary>
    /// Scheme default port for a null <see cref="EndpointDescriptor.Port"/>. TCP schemes have
    /// no well-known default, so they return null and require an explicit port.
    /// </summary>
    public static ushort? GetDefaultPort(EndpointScheme scheme) => scheme switch
    {
        EndpointScheme.Http => 80,
        EndpointScheme.Https => 443,
        _ => null
    };

    /// <summary>
    /// Hosts allow only ASCII letters, digits and . - : [ ] % _ (DNS names, IPv4 and
    /// bracketed IPv6 literals incl. zone id). IDN must arrive punycode-encoded: rejecting
    /// everything else (spaces, path separators, Unicode) keeps the rule deterministic and
    /// closes URI-confusion tricks without pulling URI parsing into the contract.
    /// </summary>
    private static bool HasValidHostCharacters(string host)
    {
        foreach (char c in host)
        {
            if (char.IsAsciiLetterOrDigit(c))
                continue;
            if (c is '.' or '-' or ':' or '[' or ']' or '%' or '_')
                continue;
            return false;
        }

        return true;
    }
}
