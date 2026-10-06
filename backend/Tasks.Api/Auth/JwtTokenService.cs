using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tasks.Api.Data;

namespace Tasks.Api.Auth;

public sealed class JwtTokenService
{
    private readonly JwtOptions options;

    /// <summary>
    /// Stores the JWT settings used to sign access tokens.
    /// </summary>
    /// <param name="options">The configured issuer, audience, key, and lifetime.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        this.options = options.Value;
    }

    /// <summary>
    /// Signs a short-lived token for the user and returns it with the public profile.
    /// </summary>
    /// <param name="user">The account that just registered or signed in.</param>
    /// <returns>The bearer token, its expiry, and the user profile.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public AuthResponse CreateResponse(ApplicationUser user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.ExpiresMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("name", user.DisplayName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            UserResponse.From(user));
    }
}
