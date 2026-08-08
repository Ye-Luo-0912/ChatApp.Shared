using System.Text.Json.Serialization;

namespace ChatApp.Auth.Contracts;

/// <summary>
/// Versioned value stored for an access token in the shared Redis cache.
/// Property names are part of the cross-process contract and must remain stable.
/// </summary>
public sealed class AccessTokenCacheRecord
{
    [JsonPropertyName("u")]
    public required long UserId { get; set; }

    /// <summary>Legacy identity snapshot retained for old cache entries.</summary>
    [JsonPropertyName("n")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserName { get; set; }

    /// <summary>Legacy role snapshot retained for old cache entries.</summary>
    [JsonPropertyName("r")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Roles { get; set; }

    [JsonPropertyName("e")]
    public required long ExpiresAtMs { get; set; }

    [JsonPropertyName("s")]
    public string? SessionId { get; set; }

    /// <summary>
    /// Legacy server-issued device identifier. Current writers use
    /// <see cref="DeviceIdHash"/> and leave this field absent.
    /// </summary>
    [JsonPropertyName("did")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeviceId { get; set; }

    [JsonPropertyName("d")]
    public ulong? DeviceIdHash { get; set; }

    [JsonPropertyName("v")]
    public long SecurityVersion { get; set; }

    /// <summary>Legacy authorization snapshot retained for old cache entries.</summary>
    [JsonPropertyName("a")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public AccessTokenAccountState AccountState { get; set; }
}
