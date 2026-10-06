using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Tasks.Api.Data;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>
    /// Creates the in-memory context used for users.
    /// </summary>
    /// <param name="options">The EF Core options, including the database name.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}
