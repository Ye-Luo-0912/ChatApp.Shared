using System.Text.Json;
using ChatApp.Auth.Contracts;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

public sealed class AccessTokenCacheContractTests
{
    private const string Token = "abc";
    private const string ExpectedHash =
        "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD";

    [Fact]
    public void CacheKeyMatchesServerAndGatewayGoldenValue()
    {
        Assert.Equal($"AT:{ExpectedHash}", AccessTokenCacheKey.CreateLogical(Token));
        Assert.Equal($"AT:{ExpectedHash}", AccessTokenCacheKey.CreateLogical(Token.AsSpan()));
        Assert.Equal($"cache:AT:{ExpectedHash}", AccessTokenCacheKey.Create(Token));
    }

    [Fact]
    public void SpanCacheKeyUsesUtf8AndMatchesStringOverload()
    {
        const string token = "令牌-token";

        Assert.Equal(
            AccessTokenCacheKey.CreateLogical(token),
            AccessTokenCacheKey.CreateLogical(token.AsSpan()));
    }

    [Fact]
    public void CurrentServerValueMatchesGoldenJson()
    {
        var value = new AccessTokenCacheRecord
        {
            UserId = 42,
            ExpiresAtMs = 1_735_689_600_123,
            SessionId = "session-42",
            DeviceIdHash = 123,
            SecurityVersion = 7,
            AccountState = AccessTokenAccountState.Active
        };

        const string expected =
            "{\"u\":42,\"e\":1735689600123,\"s\":\"session-42\",\"d\":123,\"v\":7}";

        Assert.Equal(
            expected,
            JsonSerializer.Serialize(
                value,
                AuthContractsJsonSerializerContext.Default.AccessTokenCacheRecord));
    }

    [Fact]
    public void LegacyOptionalFieldsRemainReadable()
    {
        const string json =
            "{\"u\":42,\"n\":\"legacy-name\",\"r\":[\"Admin\"],\"e\":1735689600123,\"s\":\"session-42\",\"did\":\"device-42\",\"d\":123,\"v\":7,\"a\":1}";

        AccessTokenCacheRecord? value = JsonSerializer.Deserialize(
            json,
            AuthContractsJsonSerializerContext.Default.AccessTokenCacheRecord);

        Assert.NotNull(value);
        Assert.Equal(42, value.UserId);
        Assert.Equal("legacy-name", value.UserName);
        Assert.NotNull(value.Roles);
        Assert.Equal(["Admin"], value.Roles);
        Assert.Equal("session-42", value.SessionId);
        Assert.Equal("device-42", value.DeviceId);
        Assert.Equal(123UL, value.DeviceIdHash);
        Assert.Equal(7, value.SecurityVersion);
        Assert.Equal(AccessTokenAccountState.DeletionPending, value.AccountState);
    }

    [Fact]
    public void AccountStateNumericValuesRemainStable()
    {
        Assert.Equal((short)0, (short)AccessTokenAccountState.Active);
        Assert.Equal((short)1, (short)AccessTokenAccountState.DeletionPending);
        Assert.Equal((short)2, (short)AccessTokenAccountState.Deleted);
    }
}
