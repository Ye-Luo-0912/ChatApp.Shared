using System.ComponentModel.DataAnnotations;

namespace ChatApp.Contracts.Http.Friends;

public static class FriendshipInputLimits
{
    public const int FriendRequestMessageMaxLength = 500;
    public const int FriendNoteMaxLength = 100;
}

public enum FriendshipOperationErrorCode : byte
{
    None = 0,
    Success = 1,
    ValidationFailed = 2,
    FriendshipRequestAlreadyExists = 3,
    FriendshipAlreadyExists = 4,
    FriendshipRequestNotFound = 5,
    FriendshipNotFound = 6,
    InsufficientPermissions = 7,
    InternalSystemError = 8,
    FriendshipRequestExpired = 9,
    RequestAlreadyBlocked = 10,
    FriendGroupNotFound = 11,
    FriendGroupNameConflict = 12,
    FriendRequestRejectedByPrivacy = 13
}

public enum FriendRequestStatus : byte
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Withdrawn = 3
}

public enum SendFriendRequestOutcome
{
    None = 0,
    RequestSent = 1,
    RequestAlreadyPending = 2,
    AcceptedDirectly = 3,
    RestoredDirectly = 4,
    FriendshipRestored = 5
}

public sealed class SendFriendRequestRequest
{
    [Range(1, long.MaxValue)]
    public long TargetUserId { get; set; }

    [StringLength(FriendshipInputLimits.FriendRequestMessageMaxLength)]
    public string? Message { get; set; }
}

public class FriendDto
{
    public long FriendId { get; set; }
    public string? FriendName { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AvatarUrl { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public DateTime? LastInteractionAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
}

public sealed class BlockedUserDto
{
    public long UserId { get; set; }
    public string? UserName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime BlockedAt { get; set; }
}

public sealed class FriendRequestDto
{
    public long RequestId { get; set; }
    public long RequesterId { get; set; }
    public long TargetUserId { get; set; }
    public string? Message { get; set; }
    public FriendRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Direct non-generic failure/success body emitted by friendship endpoints.</summary>
public class FriendshipOperationResponse
{
    public bool IsSuccess { get; set; }
    public FriendshipOperationErrorCode ErrorCode { get; set; }
    public string? Message { get; set; }
}

/// <summary>Direct generic service result used on friendship failure paths.</summary>
public sealed class FriendshipGenericOperationResponse<T>
{
    public bool Succeeded { get; set; }
    public FriendshipOperationErrorCode ErrorCode { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

public sealed class SendFriendRequestResponse : FriendshipOperationResponse
{
    public SendFriendRequestOutcome Outcome { get; set; }
    public FriendDto? Friend { get; set; }
}
