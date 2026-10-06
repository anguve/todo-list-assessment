using Tasks.Api.Data;

namespace Tasks.Api.Auth;

public sealed record RegisterRequest(string? Email, string? Password, string? DisplayName);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record UserResponse(string Id, string Email, string DisplayName)
{
    /// <summary>
    /// Copies the public profile fields from a stored user.
    /// </summary>
    /// <param name="user">The account to describe.</param>
    /// <returns>The id, email, and display name.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static UserResponse From(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName);
}

public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, UserResponse User);
