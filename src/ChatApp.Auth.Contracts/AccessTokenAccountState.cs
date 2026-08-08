namespace ChatApp.Auth.Contracts;

/// <summary>
/// Account lifecycle snapshot carried by legacy access-token cache entries.
/// New authorization decisions should use the authoritative user snapshot.
/// </summary>
public enum AccessTokenAccountState : short
{
    Active = 0,
    DeletionPending = 1,
    Deleted = 2,
}
