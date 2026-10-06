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

    private static readonly string DummyPasswordHash = new PasswordHasher<ApplicationUser>()
        .HashPassword(new ApplicationUser(), "timing-equalizer-not-a-user-password");

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
        group.MapPost("/login", Login);
        group.MapGet("/me", Me).RequireAuthorization();

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
    /// Signs a user in. Unknown emails and bad passwords return the same 401.
    /// </summary>
    /// <param name="request">The submitted email and password.</param>
    /// <param name="users">The Identity user store.</param>
    /// <param name="passwordHasher">Used to keep the failure path a similar length.</param>
    /// <param name="tokens">The token signer.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="httpContext">The current request, used for the audit line.</param>
    /// <returns>200 with a token, or 401.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static async Task<IResult> Login(
        LoginRequest request,
        UserManager<ApplicationUser> users,
        IPasswordHasher<ApplicationUser> passwordHasher,
        JwtTokenService tokens,
        ILoggerFactory loggerFactory,
        HttpContext httpContext)
    {
        var logger = loggerFactory.CreateLogger(ActivityLog.Category);
        var emailIsValid = FieldRules.TryEmail(request.Email, out var email, out _);
        var password = request.Password ?? string.Empty;
        var user = emailIsValid ? await users.FindByEmailAsync(email) : null;

        if (user is null)
        {
            passwordHasher.VerifyHashedPassword(new ApplicationUser(), DummyPasswordHash, password);
            ActivityLog.SignInFailed(logger, httpContext);
            return InvalidCredentials();
        }

        if (!await users.CheckPasswordAsync(user, password))
        {
            ActivityLog.SignInFailed(logger, httpContext);
            return InvalidCredentials();
        }

        ActivityLog.SignInSucceeded(logger, user.Id, httpContext);
        return Results.Ok(tokens.CreateResponse(user));
    }

    /// <summary>
    /// Returns the profile of the user named by the token.
    /// </summary>
    /// <param name="principal">The authenticated caller.</param>
    /// <param name="users">The Identity user store.</param>
    /// <returns>200 with the profile, or 401.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static async Task<IResult> Me(ClaimsPrincipal principal, UserManager<ApplicationUser> users)
    {
        var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var user = await users.FindByIdAsync(userId);
        return user is null ? Results.Unauthorized() : Results.Ok(UserResponse.From(user));
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
