namespace Tasks.Api.Logging;

public static class ThrottleLimits
{
    public const int TestingPermitLimit = 10_000;
    public const int AuthPermitLimit = 8;
    public const int ApiPermitLimit = 120;

    /// <summary>
    /// Chooses the permit count for this request. Tests use a high limit unless they opt in.
    /// </summary>
    /// <param name="httpContext">The current request.</param>
    /// <param name="key">The configuration key, such as Throttle:AuthPermitLimit.</param>
    /// <param name="liveDefault">The limit used outside the test host when config is missing.</param>
    /// <returns>How many requests are allowed in the window.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static int Resolve(HttpContext httpContext, string key, int liveDefault)
    {
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        if (environment.IsEnvironment("Testing")
            && !configuration.GetValue("Throttle:UseConfiguredLimits", false))
        {
            return TestingPermitLimit;
        }

        return configuration.GetValue(key, liveDefault);
    }

    /// <summary>
    /// Uses the remote IP so one client cannot spend another client's allowance.
    /// </summary>
    /// <param name="httpContext">The current request.</param>
    /// <returns>The IP, or "local" when it is missing.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static string PartitionKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "local";
}
