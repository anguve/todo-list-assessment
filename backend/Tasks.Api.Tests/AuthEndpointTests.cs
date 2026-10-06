using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Tasks.Api.Tests;

public sealed class AuthEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    /// <summary>Keeps the shared test host.</summary>
    /// <param name="factory">The application factory.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public AuthEndpointTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    /// <summary>A valid registration returns 201, and the same email returns 409.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Valid_registration_returns_201_and_the_same_email_returns_409()
    {
        using var client = factory.CreateClient();

        var created = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = "  Ada@Example.COM ", password = "password1", displayName = "Ada" },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var auth = await created.Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options);
        Assert.False(string.IsNullOrWhiteSpace(auth?.Token));
        Assert.Equal("ada@example.com", auth!.User.Email);
        Assert.DoesNotContain("password1", Payload(auth.Token), StringComparison.Ordinal);

        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.Token);
        var me = await client.GetFromJsonAsync<UserResponse>("/api/auth/me", TestJson.Options);
        Assert.Equal("ada@example.com", me?.Email);
        Assert.Equal("Ada", me?.DisplayName);

        var duplicate = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = "ADA@example.com", password = "password1", displayName = "Ada" },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", "password1")]
    [InlineData("weak@example.com", "short1")]
    [InlineData("weak@example.com", "longpassword")]
    [InlineData("weak@example.com", "12345678")]
    /// <summary>An invalid email or a weak password returns 400 and does not echo the password.</summary>
    /// <param name="email">The email under test.</param>
    /// <param name="password">The password under test.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Invalid_email_or_weak_password_returns_400(string email, string password)
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, password, displayName = "Ada" },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(password, body, StringComparison.Ordinal);
    }

    [Fact]
    /// <summary>A valid login returns a token, and both bad-credential cases return the same 401.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Valid_login_returns_a_token_and_bad_credentials_return_401()
    {
        using var client = factory.CreateClient();
        const string email = "login@example.com";
        const string password = "password1";

        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, password, displayName = "Ada" },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);

        var loggedIn = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "Login@Example.com", password },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
        var auth = await loggedIn.Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options);
        Assert.False(string.IsNullOrWhiteSpace(auth?.Token));

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = "wrongpass1" },
            TestJson.Options);
        var unknownEmail = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "missing@example.com", password },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);

        var wrongBody = await wrongPassword.Content.ReadFromJsonAsync<ProblemBody>(TestJson.Options);
        var unknownBody = await unknownEmail.Content.ReadFromJsonAsync<ProblemBody>(TestJson.Options);
        Assert.Equal(wrongBody?.Detail, unknownBody?.Detail);
        Assert.Equal("Email or password is incorrect", wrongBody?.Detail);
        Assert.DoesNotContain(email, wrongBody?.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("missing@example.com", unknownBody?.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    /// <summary>A display name that is too short returns 400 and does not echo the password.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task A_name_that_is_too_short_returns_400()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = "named@example.com", password = "password1", displayName = "A" },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Enter your name.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("password1", body, StringComparison.Ordinal);
    }

    [Fact]
    /// <summary>The current-user route rejects a caller who has no token.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Me_without_a_token_returns_401()
    {
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    /// <summary>Decodes the middle part of a JWT so the test can check it has no password.</summary>
    /// <param name="token">The bearer token.</param>
    /// <returns>The payload text.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static string Payload(string token)
    {
        var part = token.Split('.')[1];
        var padded = part.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
