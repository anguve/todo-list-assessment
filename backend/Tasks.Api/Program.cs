using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Tasks.Api.Auth;
using Tasks.Api.Data;
using Tasks.Api.Logging;
using Tasks.Api.Todos;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
jwtOptions.EnsureValid();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var databaseName = builder.Configuration["DatabaseName"];
if (string.IsNullOrWhiteSpace(databaseName))
{
    databaseName = "Tasks";
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.RemoveAll<ILookupNormalizer>();
builder.Services.AddSingleton<ILookupNormalizer, LowercaseLookupNormalizer>();
builder.Services.RemoveAll<IPasswordValidator<ApplicationUser>>();
builder.Services.AddScoped<IPasswordValidator<ApplicationUser>, LetterAndDigitPasswordValidator>();

builder.Services.AddScoped<ITodoRepository, TodoRepository>();

builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddSingleton<RevokedTokens>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var tokenId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                var revoked = context.HttpContext.RequestServices.GetRequiredService<RevokedTokens>();
                if (string.IsNullOrEmpty(tokenId) || revoked.IsRevoked(tokenId))
                {
                    context.Fail("The token is no longer valid.");
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        var logger = httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(ActivityLog.Category);
        ActivityLog.Throttled(logger, httpContext);

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await httpContext.Response.WriteAsJsonAsync(
            new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many attempts",
                Detail = "Wait a moment and try again.",
            },
            cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            ThrottleLimits.PartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = ThrottleLimits.Resolve(httpContext, "Throttle:ApiPermitLimit", ThrottleLimits.ApiPermitLimit),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            ThrottleLimits.PartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = ThrottleLimits.Resolve(httpContext, "Throttle:AuthPermitLimit", ThrottleLimits.AuthPermitLimit),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Warning);

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Logging.AddProvider(new AuditFileLogger(
        Path.Combine(builder.Environment.ContentRootPath, "logs", "fieldbook.log")));
}

var app = builder.Build();

app.UseMiddleware<ErrorCaptureMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DevelopmentDataSeeder.SeedAsync(app.Services);
}

app.UseCors("frontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapAuthEndpoints();

app.MapTodoEndpoints();

app.Run();

public partial class Program;
