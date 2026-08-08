using System.ComponentModel.DataAnnotations;
using ChatApp.Contracts.Http.Common;

namespace ChatApp.Contracts.Http.Auth;

public sealed class LoginRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public sealed class LogoutRequest
{
    [Required]
    [StringLength(512, MinimumLength = 16)]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class RegisterRequest
{
    [StringLength(256)]
    public string? Username { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(8, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    [Range(1, long.MaxValue)]
    public long UserId { get; set; }

    [Required]
    [StringLength(512, MinimumLength = 16)]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class SendEmailCodeRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}

public enum LoginCheckStatus : byte
{
    Success = 1,
    InvalidCredentials = 2,
    LockedOut = 3,
    NotAllowed = 4,
    RequiresTwoFactor = 5,
    Overloaded = 6
}

/// <summary>HTTP presence snapshot. Value 4 means DoNotDisturb, not Invisible.</summary>
public enum UserPresenceStatus : byte
{
    Offline = 0,
    Online = 1,
    Away = 2,
    Busy = 3,
    DoNotDisturb = 4
}

public enum AccountLifecycleState : short
{
    Active = 0,
    DeletionPending = 1,
    Deleted = 2
}

public enum AuthErrorType : byte
{
    InvalidCredentials = 0,
    TokenExpired = 1,
    DeviceMismatch = 2,
    SystemError = 3
}

/// <summary>Complete response emitted by login and MFA verification.</summary>
public sealed class LoginResponse
{
    public bool IsSuccess { get; init; }
    public LoginCheckStatus LoginCheckStatus { get; init; }
    public string? ErrorMessage { get; init; }
    public string? AccessToken { get; init; }
    public DateTime AccessTokenExpiresAtUtc { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime RefreshTokenExpiresAtUtc { get; init; }
    public DateTimeOffset LoginAt { get; init; }
    public DateTimeOffset? PreviousLoginDate { get; init; }
    public string? ClientIp { get; init; }
    public bool IsNewDevice { get; init; }
    public bool IsUnusualLocation { get; init; }
    public string? TrustedDeviceToken { get; init; }
    public string? DeviceCredential { get; init; }
    public bool RequiresRecoveryCodeRegeneration { get; init; }
    public string? SessionId { get; init; }
    public ulong? DeviceIdHash { get; init; }
    public string? MfaToken { get; init; }
    public bool RequiresTwoFactor { get; init; }
    public long? UserId { get; init; }
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Signature { get; init; }
    public bool Gender { get; init; }
    public string? Region { get; init; }
    public UserPresenceStatus Status { get; init; }
    public AccountLifecycleState AccountState { get; init; }
    public DateTimeOffset? DeletionScheduledAt { get; init; }
    public ServerEndpoint? Server { get; init; }
}

/// <summary>Rotated token pair. Both expiries and the rotated device credential are mandatory wire data.</summary>
public sealed class RefreshTokenResponse
{
    public bool IsSuccess { get; init; }
    public string? AccessToken { get; init; }
    public DateTime AccessTokenExpiresAtUtc { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime RefreshTokenExpiresAtUtc { get; init; }
    public string? DeviceCredential { get; init; }
    public AuthErrorType? ErrorType { get; init; }
}

public sealed class RegisterResponse
{
    public bool IsSuccess { get; init; }
    public long? UserId { get; init; }
    public string? Username { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<RegistrationError>? Errors { get; init; }
}

public sealed class RegistrationError
{
    public string? Code { get; init; }
    public string? Description { get; init; }
}

public sealed class EmailResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
