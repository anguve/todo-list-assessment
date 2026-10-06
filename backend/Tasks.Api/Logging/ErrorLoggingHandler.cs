using Microsoft.AspNetCore.Diagnostics;

namespace Tasks.Api.Logging;

public sealed class ErrorLoggingHandler(ILogger<ErrorLoggingHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Logs an unhandled exception and returns a generic 500. The client does not see the exception text.
    /// </summary>
    /// <param name="httpContext">The current request.</param>
    /// <param name="exception">The exception that escaped the pipeline.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>True, because this handler writes the response.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var request = RequestFacts.From(httpContext);
        logger.LogError(
            exception,
            "Unhandled error. Ip {Ip} {Method} {Path} Client {Client} Trace {TraceId}",
            request.Ip,
            request.Method,
            request.Path,
            request.Client,
            request.TraceId);

        await Results.Problem(
                title: "Unexpected error",
                detail: "The request could not be completed.",
                statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(httpContext);

        return true;
    }
}
