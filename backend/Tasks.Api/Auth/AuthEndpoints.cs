using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Tasks.Api.Data;
using Tasks.Api.Logging;
using Tasks.Api.Validation;

namespace Tasks.Api.Auth;

public static class AuthEndpoints
{
    private const string InvalidCredentialsMessage = "Email or password is incorrect";

    /// <summary>
    /// Maps registration, sign-in, sign-out, and the current-user route.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The auth route group.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", Register);

        return group;
    }

    /// <summary>
    /// Creates an account and returns a token. Duplicate emails return 409.
    /// </summary>
    /// <param name="request">The submitted email, password, and display name.</param>
    /// <param name="users">The Identity user store.</param>
    /// <param name="tokens">The token signer.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="httpContext">The current request, used for the audit line.</param>
    /// <returns>201 with a token, 400 for invalid input, or 409 when the email is taken.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static async Task<IResult> Register(
        RegisterRequest request,
        UserManager<ApplicationUser> users,
        JwtTokenService tokens,
        ILoggerFactory loggerFactory,
        HttpContext httpContext)
    {
        var logger = loggerFactory.CreateLogger(ActivityLog.Category);
        var problems = new Dictionary<string, string[]>();

        if (!FieldRules.TryEmail(request.Email, out var email, out var emailError))
        {
            problems["email"] = [emailError];
        }

        if (!FieldRules.TryPassword(request.Password, out var passwordError))
        {
            problems["password"] = [passwordError];
        }

        if (!FieldRules.TryName(request.DisplayName, out var displayName, out var nameError))
        {
            problems["displayName"] = [nameError];
        }

        if (problems.Count > 0)
        {
            ActivityLog.RegistrationRejected(logger, "validation", httpContext);
            return Results.ValidationProblem(problems);
        }

        if (await users.FindByEmailAsync(email) is not null)
        {
            ActivityLog.RegistrationRejected(logger, "email_taken", httpContext);
            return EmailTaken();
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
        };

        var result = await users.CreateAsync(user, request.Password!);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail"))
            {
                ActivityLog.RegistrationRejected(logger, "email_taken", httpContext);
                return EmailTaken();
            }

            ActivityLog.RegistrationRejected(
                logger,
                string.Join(", ", result.Errors.Select(error => error.Code)),
                httpContext);

            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["password"] = result.Errors.Select(error => error.Description).ToArray(),
            });
        }

        ActivityLog.Registered(logger, user.Id, httpContext);
        return Results.Created("/api/auth/me", tokens.CreateResponse(user));
    }

    /// <summary>
    /// Returns the 409 used when an email is already registered.
    /// </summary>
    /// <returns>A problem response.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static IResult EmailTaken() =>
        Results.Problem(
            title: "Email already registered",
            detail: "An account with that email already exists.",
            statusCode: StatusCodes.Status409Conflict);

    /// <summary>
    /// Returns the generic 401 used for every bad sign-in.
    /// </summary>
    /// <returns>A problem response.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static IResult InvalidCredentials() =>
        Results.Problem(
            title: "Unauthorized",
            detail: InvalidCredentialsMessage,
            statusCode: StatusCodes.Status401Unauthorized);
}
