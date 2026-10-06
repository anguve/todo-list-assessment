using Microsoft.AspNetCore.Identity;
using Tasks.Api.Logging;

namespace Tasks.Api.Data;

public static class DevelopmentDataSeeder
{
    public const string Email = "demo@fieldbook.test";
    public const string Password = "Demo1234";
    public const string DisplayName = "Ada Demo";

    /// <summary>
    /// Creates the documented development user when it is not already there.
    /// </summary>
    /// <param name="services">The application services.</param>
    /// <returns>A task that finishes after the user exists.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await users.FindByEmailAsync(Email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = Email,
            Email = Email,
            DisplayName = DisplayName,
        };

        var result = await users.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            var codes = string.Join(", ", result.Errors.Select(error => error.Code));
            throw new InvalidOperationException($"Could not create the development user. Codes: {codes}");
        }

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(ActivityLog.Category);
        ActivityLog.Seeded(logger, user.Id);
    }
}
