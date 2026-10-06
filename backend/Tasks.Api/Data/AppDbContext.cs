using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tasks.Api.Todos;

namespace Tasks.Api.Data;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>
    /// Creates the in-memory context used for users and tasks.
    /// </summary>
    /// <param name="options">The EF Core options, including the database name.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<TodoItem> Todos => Set<TodoItem>();

    /// <summary>
    /// Maps tasks to the signed-in user and keeps the title and description within the field rules.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(todo => todo.Id);
            entity.Property(todo => todo.Title).HasMaxLength(120).IsRequired();
            entity.Property(todo => todo.Description).HasMaxLength(400).IsRequired();
            entity.Property(todo => todo.UserId).IsRequired();
            entity.HasIndex(todo => todo.UserId);
        });
    }
}
