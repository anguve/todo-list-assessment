using Microsoft.EntityFrameworkCore;
using Tasks.Api.Data;

namespace Tasks.Api.Todos;

public sealed class TodoRepository : ITodoRepository
{
    private readonly AppDbContext db;

    /// <summary>
    /// Stores the database used by the task queries.
    /// </summary>
    /// <param name="db">The application database context.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public TodoRepository(AppDbContext db)
    {
        this.db = db;
    }

    /// <summary>
    /// Lists the tasks that belong to one user, oldest first.
    /// </summary>
    /// <param name="userId">The user id taken from the token.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>That user's tasks.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(string userId, CancellationToken cancellationToken)
    {
        return await db.Todos
            .AsNoTracking()
            .Where(todo => todo.UserId == userId)
            .OrderBy(todo => todo.CreatedAt)
            .ThenBy(todo => todo.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Stores one task.
    /// </summary>
    /// <param name="item">The task to save. Its user id must already be set.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The stored task.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task<TodoItem> AddAsync(TodoItem item, CancellationToken cancellationToken)
    {
        db.Todos.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    /// <summary>
    /// Replaces the title of a task only when it belongs to the given user.
    /// </summary>
    /// <param name="id">The task id.</param>
    /// <param name="userId">The user id taken from the token.</param>
    /// <param name="title">The cleaned title.</param>
    /// <param name="description">The cleaned description.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The updated task, or null when it does not belong to the user.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task<TodoItem?> UpdateAsync(
        Guid id,
        string userId,
        string title,
        string description,
        CancellationToken cancellationToken)
    {
        var item = await db.Todos.FirstOrDefaultAsync(
            todo => todo.Id == id && todo.UserId == userId,
            cancellationToken);

        if (item is null)
        {
            return null;
        }

        item.Title = title;
        item.Description = description;
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    /// <summary>
    /// Deletes a task only when it belongs to the given user.
    /// </summary>
    /// <param name="id">The task id.</param>
    /// <param name="userId">The user id taken from the token.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>True when a matching task was removed.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task<bool> DeleteAsync(Guid id, string userId, CancellationToken cancellationToken)
    {
        var item = await db.Todos.FirstOrDefaultAsync(
            todo => todo.Id == id && todo.UserId == userId,
            cancellationToken);

        if (item is null)
        {
            return false;
        }

        db.Todos.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
