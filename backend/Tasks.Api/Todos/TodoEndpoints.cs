using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tasks.Api.Logging;
using Tasks.Api.Validation;

namespace Tasks.Api.Todos;

public static class TodoEndpoints
{
    /// <summary>
    /// Maps the task list, create, edit, and delete routes. All of them require a token.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The task route group.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static RouteGroupBuilder MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos").RequireAuthorization();

        group.MapGet("/", List);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return group;
    }

    /// <summary>
    /// Returns only the tasks that belong to the signed-in user.
    /// </summary>
    /// <param name="principal">The authenticated caller.</param>
    /// <param name="todos">The task store.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>200 with that user's tasks, or 401.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static async Task<IResult> List(
        ClaimsPrincipal principal,
        ITodoRepository todos,
        CancellationToken cancellationToken)
    {
        var userId = UserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var items = await todos.GetAllAsync(userId, cancellationToken);
        return Results.Ok(items.Select(TodoResponse.From));
    }

    /// <summary>
    /// Stores a task for the signed-in user. The user id comes from the token.
    /// </summary>
    /// <param name="request">The submitted title and description.</param>
    /// <param name="principal">The authenticated caller.</param>
    /// <param name="todos">The task store.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="httpContext">The current request, used for the audit line.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>201 with the task, 400 for a bad title or description, or 401.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static async Task<IResult> Create(
        CreateTodoRequest request,
        ClaimsPrincipal principal,
        ITodoRepository todos,
        ILoggerFactory loggerFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = UserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var problems = new Dictionary<string, string[]>();
        var titleOk = FieldRules.TryTitle(request.Title, out var title, out var titleError);
        var descriptionOk = FieldRules.TryDescription(
            request.Description,
            out var description,
            out var descriptionError);
        if (!titleOk)
        {
            problems["title"] = [titleError];
        }

        if (!descriptionOk)
        {
            problems["description"] = [descriptionError];
        }

        if (problems.Count > 0)
        {
            return Results.ValidationProblem(problems);
        }

        var item = await todos.AddAsync(
            new TodoItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            cancellationToken);

        ActivityLog.TaskCreated(
            loggerFactory.CreateLogger(ActivityLog.Category),
            userId,
            item.Id,
            httpContext);

        return Results.Created($"/api/todos/{item.Id}", TodoResponse.From(item));
    }

    /// <summary>
    /// Replaces the title and description of one of the signed-in user's tasks. Someone else's task returns 404.
    /// </summary>
    /// <param name="id">The task id from the route.</param>
    /// <param name="request">The submitted title and description.</param>
    /// <param name="principal">The authenticated caller.</param>
    /// <param name="todos">The task store.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="httpContext">The current request, used for the audit line.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>200 with the task, 400 for a bad title or description, 404, or 401.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static async Task<IResult> Update(
        Guid id,
        CreateTodoRequest request,
        ClaimsPrincipal principal,
        ITodoRepository todos,
        ILoggerFactory loggerFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = UserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var problems = new Dictionary<string, string[]>();
        var titleOk = FieldRules.TryTitle(request.Title, out var title, out var titleError);
        var descriptionOk = FieldRules.TryDescription(
            request.Description,
            out var description,
            out var descriptionError);
        if (!titleOk)
        {
            problems["title"] = [titleError];
        }

        if (!descriptionOk)
        {
            problems["description"] = [descriptionError];
        }

        if (problems.Count > 0)
        {
            return Results.ValidationProblem(problems);
        }

        var logger = loggerFactory.CreateLogger(ActivityLog.Category);
        var item = await todos.UpdateAsync(id, userId, title, description, cancellationToken);
        if (item is null)
        {
            ActivityLog.TaskUpdateMissed(logger, userId, id, httpContext);
            return Results.Problem(
                title: "Task not found",
                detail: "That task does not exist.",
                statusCode: StatusCodes.Status404NotFound);
        }

        ActivityLog.TaskUpdated(logger, userId, item.Id, httpContext);
        return Results.Ok(TodoResponse.From(item));
    }

    /// <summary>
    /// Deletes one of the signed-in user's tasks. Someone else's task returns 404.
    /// </summary>
    /// <param name="id">The task id from the route.</param>
    /// <param name="principal">The authenticated caller.</param>
    /// <param name="todos">The task store.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="httpContext">The current request, used for the audit line.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>204, 404, or 401.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static async Task<IResult> Delete(
        Guid id,
        ClaimsPrincipal principal,
        ITodoRepository todos,
        ILoggerFactory loggerFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = UserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var logger = loggerFactory.CreateLogger(ActivityLog.Category);
        var deleted = await todos.DeleteAsync(id, userId, cancellationToken);
        if (!deleted)
        {
            ActivityLog.TaskDeleteMissed(logger, userId, id, httpContext);
            return Results.Problem(
                title: "Task not found",
                detail: "That task does not exist.",
                statusCode: StatusCodes.Status404NotFound);
        }

        ActivityLog.TaskDeleted(logger, userId, id, httpContext);
        return Results.NoContent();
    }

    /// <summary>
    /// Reads the user id from the token subject claim.
    /// </summary>
    /// <param name="principal">The authenticated caller.</param>
    /// <returns>The user id, or null when the claim is missing.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static string? UserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
}

public sealed record CreateTodoRequest(string? Title, string? Description);

public sealed record TodoResponse(Guid Id, string Title, string Description, DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Copies a stored task into the response shape.
    /// </summary>
    /// <param name="item">The stored task.</param>
    /// <returns>The id, title, description, and creation time.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static TodoResponse From(TodoItem item) =>
        new(item.Id, item.Title, item.Description, item.CreatedAt);
}
