namespace Tasks.Api.Logging;

public sealed class ErrorCaptureMiddleware(RequestDelegate next, ILogger<ErrorCaptureMiddleware> logger)
{
    /// <summary>
    /// Logs every finished response whose status is 400 or higher.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <returns>A task that finishes after the rest of the pipeline.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.StatusCode < StatusCodes.Status400BadRequest)
        {
            return;
        }

        var request = RequestFacts.From(context);
        logger.LogWarning(
            "Request failed. Status {StatusCode} Ip {Ip} {Method} {Path} Client {Client} Trace {TraceId}",
            context.Response.StatusCode,
            request.Ip,
            request.Method,
            request.Path,
            request.Client,
            request.TraceId);
    }
}
