using System.Text.Json.Serialization;
using ChatApp.Contracts.Http.Attachments;
using ChatApp.Contracts.Http.Auth;
using ChatApp.Contracts.Http.Common;
using ChatApp.Contracts.Http.Friends;
using ChatApp.Contracts.Http.Sessions;

namespace ChatApp.Contracts.Http;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(LogoutRequest))]
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(RefreshTokenRequest))]
[JsonSerializable(typeof(SendEmailCodeRequest))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(RefreshTokenResponse))]
[JsonSerializable(typeof(RegisterResponse))]
[JsonSerializable(typeof(EmailResult))]
[JsonSerializable(typeof(AttachmentPresignRequest))]
[JsonSerializable(typeof(AttachmentPresignResponse))]
[JsonSerializable(typeof(ConfirmAttachmentRequest))]
[JsonSerializable(typeof(ConfirmAttachmentResponse))]
[JsonSerializable(typeof(CursorPage<FriendDto>))]
[JsonSerializable(typeof(CursorPage<FriendRequestDto>))]
[JsonSerializable(typeof(CursorPage<BlockedUserDto>))]
[JsonSerializable(typeof(ApiEnvelope<FriendshipOperationResponse>))]
[JsonSerializable(typeof(ApiEnvelope<SendFriendRequestResponse>))]
[JsonSerializable(typeof(ApiEnvelope<FriendDto>))]
[JsonSerializable(typeof(FriendshipOperationResponse))]
[JsonSerializable(typeof(FriendshipGenericOperationResponse<FriendDto>))]
[JsonSerializable(typeof(SendFriendRequestRequest))]
[JsonSerializable(typeof(SendFriendRequestResponse))]
[JsonSerializable(typeof(List<SessionDevice>))]
[JsonSerializable(typeof(RevokeSessionsResponse))]
[JsonSerializable(typeof(EndpointDescriptor))]
public partial class HttpContractsJsonSerializerContext : JsonSerializerContext;
