using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Tasks.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"tasks-tests-{Guid.NewGuid():N}";

    /// <summary>Sets the test signing key and a private in-memory database name.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Jwt__Key", "test-signing-key-at-least-32-characters");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "Tasks.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "Tasks.Tests");
        Environment.SetEnvironmentVariable("Jwt__ExpiresMinutes", "30");
        Environment.SetEnvironmentVariable("DatabaseName", databaseName);
    }

    /// <summary>Points the test host at the Testing environment and captures log lines.</summary>
    /// <param name="builder">The web host builder.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.AddProvider(new LogCapture()));
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-at-least-32-characters",
                ["Jwt:Issuer"] = "Tasks.Tests",
                ["Jwt:Audience"] = "Tasks.Tests",
                ["Jwt:ExpiresMinutes"] = "30",
                ["DatabaseName"] = databaseName,
            });
        });
    }
}
