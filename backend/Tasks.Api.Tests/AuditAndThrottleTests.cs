using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace Tasks.Api.Tests;

public sealed class AuditAndThrottleTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    /// <summary>Keeps the shared test host.</summary>
    /// <param name="factory">The application factory.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public AuditAndThrottleTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    /// <summary>Sign-in and task changes are logged, and the log does not contain the password or token.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Sign_in_and_task_changes_are_logged_without_secrets()
    {
        LogCapture.Clear();
        using var client = factory.CreateClient();
        const string password = "ledger-secret-91";

        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = "ledger@example.com", password, displayName = "Ada" },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options);
        Assert.NotNull(auth);

        var loggedIn = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "ledger@example.com", password },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var created = await client.PostAsJsonAsync(
            "/api/todos",
            new { title = "File the receipt", description = "Keep the paper copy" },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options);
        Assert.NotNull(todo);

        var deleted = await client.DeleteAsync($"/api/todos/{todo.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var wrong = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "ledger@example.com", password = "wrongpass1" },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);

        var log = LogCapture.Text();
        Assert.Contains("Transaction account.registered", log, StringComparison.Ordinal);
        Assert.Contains("Sign-in succeeded", log, StringComparison.Ordinal);
        Assert.Contains("Transaction task.created", log, StringComparison.Ordinal);
        Assert.Contains("Transaction task.deleted", log, StringComparison.Ordinal);
        Assert.Contains("Sign-in failed", log, StringComparison.Ordinal);
        Assert.Contains("Request failed. Status 401", log, StringComparison.Ordinal);
        Assert.DoesNotContain(password, log, StringComparison.Ordinal);
        Assert.DoesNotContain("wrongpass1", log, StringComparison.Ordinal);
        Assert.DoesNotContain(auth.Token, log, StringComparison.Ordinal);
    }

    [Fact]
    /// <summary>The third login inside the limit window returns 429.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Login_is_throttled_after_the_permit_limit()
    {
        LogCapture.Clear();
        using var throttled = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Throttle:UseConfiguredLimits"] = "true",
                    ["Throttle:AuthPermitLimit"] = "2",
                });
            });
        });

        using var client = throttled.CreateClient();
        const string password = "throttle-secret-91";
        HttpResponseMessage? last = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            last = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { email = "nobody@example.com", password },
                TestJson.Options);
        }

        Assert.NotNull(last);
        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
        var body = await last.Content.ReadAsStringAsync();
        Assert.Contains("Wait a moment and try again.", body, StringComparison.Ordinal);
        Assert.DoesNotContain(password, body, StringComparison.Ordinal);

        var log = LogCapture.Text();
        Assert.Contains("Request throttled. POST /api/auth/login", log, StringComparison.Ordinal);
        Assert.DoesNotContain(password, log, StringComparison.Ordinal);
    }

    [Fact]
    /// <summary>An unhandled error is logged on the server and hidden from the response body.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Unhandled_errors_are_logged_and_hidden_from_the_client()
    {
        LogCapture.Clear();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/test/error");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("The request could not be completed.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("boom-marker", body, StringComparison.Ordinal);

        var log = LogCapture.Text();
        Assert.Contains("Unhandled error.", log, StringComparison.Ordinal);
        Assert.Contains("boom-marker", log, StringComparison.Ordinal);
    }
}
