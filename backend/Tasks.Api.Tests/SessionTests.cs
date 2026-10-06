using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Tasks.Api.Tests;

public sealed class SessionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    /// <summary>Keeps the shared test host.</summary>
    /// <param name="factory">The application factory.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public SessionTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    /// <summary>Sign-out revokes only that token. A later sign-in works, and another user's token keeps working.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Signing_out_revokes_the_token_and_a_new_login_works()
    {
        LogCapture.Clear();
        using var owner = await RegisterAsync("revoke@example.com", "Ada");
        using var other = await RegisterAsync("still-in@example.com", "Bea");
        var revokedToken = owner.DefaultRequestHeaders.Authorization!.Parameter!;

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/api/todos")).StatusCode);

        var logout = await owner.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await owner.GetAsync("/api/todos")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await owner.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await owner.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/api/todos")).StatusCode);

        using var fresh = factory.CreateClient();
        var loggedIn = await fresh.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "revoke@example.com", password = "password1" },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
        var auth = await loggedIn.Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options);
        Assert.NotNull(auth);
        Assert.NotEqual(revokedToken, auth!.Token);

        fresh.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        Assert.Equal(HttpStatusCode.OK, (await fresh.GetAsync("/api/todos")).StatusCode);

        var log = LogCapture.Text();
        Assert.Contains("Sign-out succeeded", log, StringComparison.Ordinal);
        Assert.DoesNotContain(revokedToken, log, StringComparison.Ordinal);
        Assert.DoesNotContain(auth.Token, log, StringComparison.Ordinal);
        Assert.DoesNotContain("password1", log, StringComparison.Ordinal);
    }

    [Fact]
    /// <summary>Sign-out without a token returns 401.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Logout_without_a_token_returns_401()
    {
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [Fact]
    /// <summary>Successful and rejected responses both carry the security headers.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Responses_include_security_headers()
    {
        using var client = factory.CreateClient();

        AssertHeaders(await client.GetAsync("/health"));
        AssertHeaders(await client.GetAsync("/api/todos"));
    }

    /// <summary>Registers a user and returns a client that sends that user's token.</summary>
    /// <param name="email">The new account email.</param>
    /// <param name="displayName">The name stored on the account.</param>
    /// <returns>An authorized client.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private async Task<HttpClient> RegisterAsync(string email, string displayName)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, password = "password1", displayName },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options);
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    /// <summary>Checks the headers that every API response should send.</summary>
    /// <param name="response">The response under test.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static void AssertHeaders(HttpResponseMessage response)
    {
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        Assert.Contains("camera=()", Header(response, "Permissions-Policy"), StringComparison.Ordinal);
        Assert.Contains("microphone=()", Header(response, "Permissions-Policy"), StringComparison.Ordinal);
        Assert.Contains("geolocation=()", Header(response, "Permissions-Policy"), StringComparison.Ordinal);
        Assert.Contains("default-src 'none'", Header(response, "Content-Security-Policy"), StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", Header(response, "Content-Security-Policy"), StringComparison.Ordinal);
        Assert.Contains("no-store", Header(response, "Cache-Control"), StringComparison.Ordinal);
    }

    /// <summary>Reads one response header, including values stored on the content headers.</summary>
    /// <param name="response">The response under test.</param>
    /// <param name="name">The header name.</param>
    /// <returns>The joined header value.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static string Header(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
        {
            return string.Join(", ", values);
        }

        if (response.Content.Headers.TryGetValues(name, out var contentValues))
        {
            return string.Join(", ", contentValues);
        }

        return string.Empty;
    }
}
