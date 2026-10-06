using System.Text;

namespace Tasks.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "Tasks";

    public string Audience { get; set; } = "Tasks.Client";

    public int ExpiresMinutes { get; set; } = 30;

    /// <summary>
    /// Fails fast when the signing key, issuer, audience, or lifetime is not usable.
    /// </summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public void EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(Key) || Encoding.UTF8.GetByteCount(Key) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key is missing or shorter than 32 characters. " +
                "Set it with `dotnet user-secrets set \"Jwt:Key\" \"<your-key>\"` " +
                "or the Jwt__Key environment variable. Do not put it in appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
        }

        if (ExpiresMinutes is < 15 or > 60)
        {
            throw new InvalidOperationException("Jwt:ExpiresMinutes must be between 15 and 60.");
        }
    }
}
