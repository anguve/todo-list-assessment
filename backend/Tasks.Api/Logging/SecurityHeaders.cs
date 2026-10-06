namespace Tasks.Api.Logging;

public static class SecurityHeaders
{
    /// <summary>
    /// Adds the response headers for a JSON API. The page itself is served by Angular.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void Apply(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
        headers["Cache-Control"] = "no-store";
    }
}
