using System.Text.Json;
using ChatApp.Contracts.Http;
using ChatApp.Contracts.Http.Attachments;
using ChatApp.Contracts.Http.Auth;
using ChatApp.Contracts.Http.Common;
using ChatApp.Contracts.Http.Friends;
using ChatApp.Contracts.Http.Sessions;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

public sealed class HttpContractJsonGoldenTests
{
    [Fact]
    public void LoginResponse_PreservesSecurityAndExpiryFields()
    {
        var value = new LoginResponse
        {
            IsSuccess = true,
            LoginCheckStatus = LoginCheckStatus.Success,
            AccessToken = "AT",
            AccessTokenExpiresAtUtc = new DateTime(2026, 8, 5, 1, 2, 3, DateTimeKind.Utc),
            RefreshToken = "RT",
            RefreshTokenExpiresAtUtc = new DateTime(2026, 9, 5, 1, 2, 3, DateTimeKind.Utc),
            DeviceCredential = "DC",
            UserId = 42,
            Status = UserPresenceStatus.DoNotDisturb,
            Server = new ServerEndpoint { Host = "gateway", Name = "cn-1", Port = 443 }
        };

        string json = JsonSerializer.Serialize(
            value,
            HttpContractsJsonSerializerContext.Default.LoginResponse);

        Assert.Equal(
            "{\"isSuccess\":true,\"loginCheckStatus\":1,\"accessToken\":\"AT\",\"accessTokenExpiresAtUtc\":\"2026-08-05T01:02:03Z\",\"refreshToken\":\"RT\",\"refreshTokenExpiresAtUtc\":\"2026-09-05T01:02:03Z\",\"loginAt\":\"0001-01-01T00:00:00+00:00\",\"isNewDevice\":false,\"isUnusualLocation\":false,\"deviceCredential\":\"DC\",\"requiresRecoveryCodeRegeneration\":false,\"requiresTwoFactor\":false,\"userId\":42,\"gender\":false,\"status\":4,\"accountState\":0,\"server\":{\"host\":\"gateway\",\"name\":\"cn-1\",\"port\":443}}",
            json);
    }

    [Fact]
    public void RefreshResponse_RequiresBothExpiriesAndRotatedCredentialOnWire()
    {
        const string json = """
            {"isSuccess":true,"accessToken":"AT2","accessTokenExpiresAtUtc":"2026-08-05T02:00:00Z","refreshToken":"RT2","refreshTokenExpiresAtUtc":"2026-09-05T02:00:00Z","deviceCredential":"DC2"}
            """;

        RefreshTokenResponse? value = JsonSerializer.Deserialize(
            json,
            HttpContractsJsonSerializerContext.Default.RefreshTokenResponse);

        Assert.NotNull(value);
        Assert.Equal("DC2", value.DeviceCredential);
        Assert.Equal(new DateTime(2026, 8, 5, 2, 0, 0, DateTimeKind.Utc), value.AccessTokenExpiresAtUtc);
        Assert.Equal(new DateTime(2026, 9, 5, 2, 0, 0, DateTimeKind.Utc), value.RefreshTokenExpiresAtUtc);
    }

    [Fact]
    public void FriendshipCursorAndEnvelope_MatchCurrentServerWire()
    {
        var page = new CursorPage<FriendDto>
        {
            Items = [new FriendDto { FriendId = 7, FriendName = "alice" }],
            NextCursor = "next",
            HasMore = true
        };
        var envelope = new ApiEnvelope<SendFriendRequestResponse>
        {
            Data = new SendFriendRequestResponse
            {
                IsSuccess = true,
                Outcome = SendFriendRequestOutcome.RequestSent
            }
        };

        Assert.Equal(
            "{\"items\":[{\"friendId\":7,\"friendName\":\"alice\",\"createdAt\":\"0001-01-01T00:00:00\"}],\"nextCursor\":\"next\",\"hasMore\":true}",
            JsonSerializer.Serialize(page, HttpContractsJsonSerializerContext.Default.CursorPageFriendDto));
        Assert.Equal(
            "{\"data\":{\"outcome\":1,\"isSuccess\":true,\"errorCode\":0}}",
            JsonSerializer.Serialize(envelope, HttpContractsJsonSerializerContext.Default.ApiEnvelopeSendFriendRequestResponse));
    }

    [Fact]
    public void AttachmentPresign_PreservesRequiredUploadHeaders()
    {
#pragma warning disable CS0618
        var value = new AttachmentPresignResponse
        {
            AttachmentId = "a1",
            UploadUrl = "https://s3/upload",
            DownloadPath = "/api/attachments/a1/download",
            ObjectKey = "quarantine/a1",
            Ticket = "ticket",
            ExpiresAt = new DateTimeOffset(2026, 8, 5, 3, 0, 0, TimeSpan.Zero),
            UploadHeaders = new Dictionary<string, string>
            {
                ["Content-Type"] = "image/png",
                ["x-amz-tagging"] = "chatapp-scan-state=unconfirmed"
            },
            PublicUrl = string.Empty
        };
#pragma warning restore CS0618

        string json = JsonSerializer.Serialize(
            value,
            HttpContractsJsonSerializerContext.Default.AttachmentPresignResponse);

        Assert.Contains("\"uploadHeaders\":{\"Content-Type\":\"image/png\",\"x-amz-tagging\":\"chatapp-scan-state=unconfirmed\"}", json);
        Assert.Contains("\"publicUrl\":\"\"", json);
    }

    [Fact]
    public void SessionList_UsesSharedWireDtoWithoutPresentationFields()
    {
        List<SessionDevice>? sessions = JsonSerializer.Deserialize(
            """
            [{"deviceId":"desktop-1","deviceName":"Workstation","lastActiveAt":"2026-08-05T03:00:00Z","isCurrent":true}]
            """,
            HttpContractsJsonSerializerContext.Default.ListSessionDevice);

        SessionDevice session = Assert.Single(sessions!);
        Assert.Equal("desktop-1", session.DeviceId);
        Assert.Equal("Workstation", session.DeviceName);
        Assert.True(session.IsCurrent);
    }

    [Fact]
    public void PublicEnumValues_AreStable()
    {
        Assert.Equal(6, (byte)LoginCheckStatus.Overloaded);
        Assert.Equal(4, (byte)UserPresenceStatus.DoNotDisturb);
        Assert.Equal(3, (byte)FriendRequestStatus.Withdrawn);
        Assert.Equal(12, (byte)FriendshipOperationErrorCode.FriendGroupNameConflict);
        Assert.Equal(13, (byte)FriendshipOperationErrorCode.FriendRequestRejectedByPrivacy);
    }
}
