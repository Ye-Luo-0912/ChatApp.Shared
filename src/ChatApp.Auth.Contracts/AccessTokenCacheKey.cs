using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace ChatApp.Auth.Contracts;

/// <summary>
/// Produces the stable logical and physical Redis keys for access-token entries.
/// </summary>
public static class AccessTokenCacheKey
{
    public const string LogicalPrefix = "AT:";
    public const string DefaultRedisPrefix = "cache:";

    /// <summary>
    /// Creates the physical Redis key used by services that access Redis directly.
    /// </summary>
    public static string Create(string token) =>
        string.Concat(DefaultRedisPrefix, CreateLogical(token));

    /// <summary>
    /// Creates the unqualified key used by cache abstractions that add their own prefix.
    /// </summary>
    public static string CreateLogical(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return CreateLogical(token.AsSpan());
    }

    /// <summary>
    /// Creates the unqualified key from caller-owned character storage without
    /// allocating an intermediate token string.
    /// </summary>
    public static string CreateLogical(ReadOnlySpan<char> token)
    {
        if (token.IsEmpty || token.Trim().IsEmpty)
        {
            throw new ArgumentException("Token must not be empty or whitespace.", nameof(token));
        }

        int byteCount = Encoding.UTF8.GetByteCount(token);
        byte[]? rented = null;
        Span<byte> tokenBytes = byteCount <= 256
            ? stackalloc byte[byteCount]
            : (rented = ArrayPool<byte>.Shared.Rent(byteCount)).AsSpan(0, byteCount);

        try
        {
            Encoding.UTF8.GetBytes(token, tokenBytes);
            Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(tokenBytes, hash);
            return string.Concat(LogicalPrefix, Convert.ToHexString(hash));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tokenBytes);
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: false);
            }
        }
    }
}
