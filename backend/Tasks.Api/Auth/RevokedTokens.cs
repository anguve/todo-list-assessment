using System.Collections.Concurrent;

namespace Tasks.Api.Auth;

public sealed class RevokedTokens
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> until = new();

    /// <summary>
    /// Remembers a token id until that token would have expired.
    /// </summary>
    /// <param name="tokenId">The JWT ID claim.</param>
    /// <param name="expiresAt">When the token stops being acceptable.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public void Revoke(string tokenId, DateTimeOffset expiresAt)
    {
        ForgetExpired();
        until[tokenId] = expiresAt;
    }

    /// <summary>
    /// Reports whether a token id was revoked and has not expired yet.
    /// </summary>
    /// <param name="tokenId">The JWT ID claim.</param>
    /// <returns>True when the token must be rejected.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public bool IsRevoked(string tokenId)
    {
        if (!until.TryGetValue(tokenId, out var expiresAt))
        {
            return false;
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            until.TryRemove(tokenId, out _);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Drops revocation records that are past the token expiry.
    /// </summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private void ForgetExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in until)
        {
            if (entry.Value <= now)
            {
                until.TryRemove(entry.Key, out _);
            }
        }
    }
}
