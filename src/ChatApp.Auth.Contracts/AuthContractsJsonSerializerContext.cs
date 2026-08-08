using System.Text.Json.Serialization;

namespace ChatApp.Auth.Contracts;

/// <summary>
/// Source-generated JSON metadata for authentication cache contracts.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(AccessTokenCacheRecord))]
public partial class AuthContractsJsonSerializerContext : JsonSerializerContext;
