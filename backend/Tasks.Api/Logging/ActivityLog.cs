namespace Tasks.Api.Logging;

public static class ActivityLog
{
    public const string Category = "Tasks.Audit";

    /// <summary>Records a successful sign-in with the user id and request facts.</summary>
    /// <param name="logger">The audit logger.</param>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="httpContext">The current request.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void SignInSucceeded(ILogger logger, string userId, HttpContext httpContext)
    {
        var request = RequestFacts.From(httpContext);
        logger.LogInformation(
            "Sign-in succeeded. User {UserId} Ip {Ip} {Method} {Path} Client {Client} Trace {TraceId}",
            userId,
            request.Ip,
            request.Method,
            request.Path,
            request.Client,
            request.TraceId);
    }

    /// <summary>Records that the current token was revoked.</summary>
    /// <param name="logger">The audit logger.</param>
    /// <param name="userId">The user who signed out.</param>
    /// <param name="httpContext">The current request.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void SignedOut(ILogger logger, string userId, HttpContext httpContext)
    {
        var request = RequestFacts.From(httpContext);
        logger.LogInformation(
            "Sign-out succeeded. User {UserId} Ip {Ip} {Method} {Path} Client {Client} Trace {TraceId}",
            userId,
            request.Ip,
            request.Method,
            request.Path,
            request.Client,
            request.TraceId);
    }

    /// <summary>Records a failed sign-in without the email or password.</summary>
    /// <param name="logger">The audit logger.</param>
    /// <param name="httpContext">The current request.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void SignInFailed(ILogger logger, HttpContext httpContext)
    {
        var request = RequestFacts.From(httpContext);
        logger.LogWarning(
            "Sign-in failed. Ip {Ip} {Method} {Path} Client {Client} Trace {TraceId}",
            request.Ip,
            request.Method,
            request.Path,
            request.Client,
            request.TraceId);
    }

    /// <summary>Records that an account was created.</summary>
    /// <param name="logger">The audit logger.</param>
    /// <param name="userId">The new user.</param>
    /// <param name="httpContext">The current request.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void Registered(ILogger logger, string userId, HttpContext httpContext)
    {
        var request = RequestFacts.From(httpContext);
        logger.LogInformation(
            "Transaction account.registered. User {UserId} Ip {Ip} {Method} {Path} Client {Client} Trace {TraceId}",
            userId,
            request.Ip,
            request.Method,
            request.Path,
            request.Client,
            request.TraceId);
    }

    /// <summary>Records a rejected registration by reason code, not by the submitted values.</summary>
    /// <param name="logger">The audit logger.</param>
    /// <param name="reason">A short reason such as validation or email_taken.</param>
    /// <param name="httpContext">The current request.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void RegistrationRejected(ILogger logger, string reason, HttpContext httpContext)
    {
        var request = RequestFacts.From(httpContext);
        logger.LogWarning(
            "Registration rejected. Reason {Reason} Ip {Ip} {Method} {Path} Client {Client} Trace {TraceId}",
            reason,
            request.Ip,
            request.Method,
            request.Path,
            request.Client,
            request.TraceId);
    }

    /// <summary>Records that the development user was created. There is no HTTP request.</summary>
    /// <param name="logger">The audit logger.</param>
    /// <param name="userId">The seeded user.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void Seeded(ILogger logger, string userId) =>
        logger.LogInformation("Transaction account.seeded. User {UserId}", userId);

    /// <summary>Records that a task was created.</summary>
    /// <param name="logger">The audit logger.</param>
    /// <param name="userId">The owner.</param>
    /// <param name="taskId">The new task.</param>
    /// <param name="httpContext">The current request.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void TaskCreated(ILogger logger, string userId, Guid taskId, HttpContext httpContext)
    {
        var request = RequestFacts.From(httpContext);
        logger.LogInformation(
            "Transaction task.created. User {UserId} Task {TaskId} Ip {Ip} {Method} {Path} Client {Client} Trace {TraceId}",
            userId,
            taskId,
            request.Ip,
            request.Method,
            request.Path,
            request.Client,
            request.TraceId);
    }

    /// <summary>Records that a task title was replaced.</summary>
    /// <param name="logger">The audit logger.</param>
    /// <param name="userId">The owner.</param>
    /// <param name="taskId">The edited task.</param>
    /// <param name="httpContext">The current request.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void TaskUpdated(ILogger logger, string userId, Guid taskId, HttpContext httpContext)
    {
        var request = RequestFacts.From(httpContext);
        logger.LogInformation(
            "Transaction task.updated. User {UserId} Task {TaskId} Ip {Ip} {Method} {Path} Client {Client} Trace {TraceId}",
            userId,
            taskId,
            request.Ip,
            request.Method,
            request.Path,
            request.Client,
            request.TraceId);
    }

    /// <summary>Records an edit that did not match the signed-in user's task.</summary>
    /// <param name="logger">The audit logger.</param>
    /// <param name="userId">The caller.</param>
    /// <param name="taskId">The task id from the route.</param>
    /// <param name="httpContext">The current request.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void TaskUpdateMissed(ILogger logger, string userId, Guid taskId, HttpContext httpContext)
    {
        var request = RequestFacts.From(httpContext);
        logger.LogWarning(
            "Transaction task.update missed. User {UserId} Task {TaskId} Ip {Ip} {Method} {Path} Client {Client} Trace {TraceId}",
            userId,
            taskId,
            request.Ip,
            request.Method,
            request.Path,
            request.Client,
            request.TraceId);
    }

    /// <summary>Records that a task was deleted.</summary>
    /// <param name="logger">The audit logger.</param>
    /// <param name="userId">The owner.</param>
    /// <param name="taskId">The deleted task.</param>
    /// <param name="httpContext">The current request.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void TaskDeleted(ILogger logger, string userId, Guid taskId, HttpContext httpContext)
    {
        var request = RequestFacts.From(httpContext);
        logger.LogInformation(
            "Transaction task.deleted. User {UserId} Task {TaskId} Ip {Ip} {Method} {Path} Client {Client} Trace {TraceId}",
            userId,
            taskId,
            request.Ip,
            request.Method,
            request.Path,
            request.Client,
            request.TraceId);
    }

    /// <summary>Records a delete that did not match the signed-in user's task.</summary>
    /// <param name="logger">The audit logger.</param>
    /// <param name="userId">The caller.</param>
    /// <param name="taskId">The task id from the route.</param>
    /// <param name="httpContext">The current request.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void TaskDeleteMissed(ILogger logger, string userId, Guid taskId, HttpContext httpContext)
    {
        var request = RequestFacts.From(httpContext);
        logger.LogWarning(
            "Transaction task.delete missed. User {UserId} Task {TaskId} Ip {Ip} {Method} {Path} Client {Client} Trace {TraceId}",
            userId,
            taskId,
            request.Ip,
            request.Method,
            request.Path,
            request.Client,
            request.TraceId);
    }

    /// <summary>Records that a request was rejected by the throttle.</summary>
    /// <param name="logger">The audit logger.</param>
    /// <param name="httpContext">The current request.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void Throttled(ILogger logger, HttpContext httpContext)
    {
        var request = RequestFacts.From(httpContext);
        logger.LogWarning(
            "Request throttled. {Method} {Path} Ip {Ip} Client {Client} Trace {TraceId}",
            request.Method,
            request.Path,
            request.Ip,
            request.Client,
            request.TraceId);
    }
}
