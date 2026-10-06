namespace Tasks.Api.Todos;

public interface ITodoRepository
{
    /// <summary>
    /// Lists the tasks that belong to one user, oldest first.
    /// </summary>
    /// <param name="userId">The user id taken from the token.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>That user's tasks.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    Task<IReadOnlyList<TodoItem>> GetAllAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Stores one task.
    /// </summary>
    /// <param name="item">The task to save. Its user id must already be set.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The stored task.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    Task<TodoItem> AddAsync(TodoItem item, CancellationToken cancellationToken);

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
    Task<TodoItem?> UpdateAsync(
        Guid id,
        string userId,
        string title,
        string description,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a task only when it belongs to the given user.
    /// </summary>
    /// <param name="id">The task id.</param>
    /// <param name="userId">The user id taken from the token.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>True when a matching task was removed.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    Task<bool> DeleteAsync(Guid id, string userId, CancellationToken cancellationToken);
}
