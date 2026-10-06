namespace Tasks.Api.Logging;

public readonly record struct RequestFacts(string Ip, string Method, string Path, string Client, string TraceId)
{
    /// <summary>
    /// Reads the IP, method, path, user agent, and trace id. It does not read the body.
    /// </summary>
    /// <param name="httpContext">The current request.</param>
    /// <returns>The facts written on audit and error lines.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static RequestFacts From(HttpContext httpContext)
    {
        var agent = httpContext.Request.Headers.UserAgent.ToString().Trim();
        if (agent.Length == 0)
        {
            agent = "unknown";
        }
        else if (agent.Length > 200)
        {
            agent = agent[..200];
        }

        return new RequestFacts(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            httpContext.Request.Method,
            httpContext.Request.Path.Value ?? "/",
            agent,
            httpContext.TraceIdentifier);
    }
}
