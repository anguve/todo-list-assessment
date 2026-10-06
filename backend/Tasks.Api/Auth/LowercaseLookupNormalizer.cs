using Microsoft.AspNetCore.Identity;

namespace Tasks.Api.Auth;

public sealed class LowercaseLookupNormalizer : ILookupNormalizer
{
    /// <summary>
    /// Trims an email and stores the lookup key in lowercase.
    /// </summary>
    /// <param name="email">The email to normalize.</param>
    /// <returns>The normalized email, or null.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public string? NormalizeEmail(string? email) => email?.Trim().ToLowerInvariant();

    /// <summary>
    /// Trims a username and stores the lookup key in lowercase.
    /// </summary>
    /// <param name="name">The username to normalize.</param>
    /// <returns>The normalized username, or null.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public string? NormalizeName(string? name) => name?.Trim().ToLowerInvariant();
}
