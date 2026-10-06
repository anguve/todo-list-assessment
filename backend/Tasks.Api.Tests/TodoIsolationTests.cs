using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Tasks.Api.Tests;

public sealed class TodoIsolationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    /// <summary>Keeps the shared test host.</summary>
    /// <param name="factory">The application factory.</param>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public TodoIsolationTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    /// <summary>Task routes reject a caller who has no token.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Todos_without_a_token_return_401()
    {
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/todos")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/todos", new { title = "Nope", description = "Nope" }, TestJson.Options)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync(
                $"/api/todos/{Guid.NewGuid()}",
                new { title = "Nope", description = "Nope" },
                TestJson.Options))
                .StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.DeleteAsync($"/api/todos/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    /// <summary>One user cannot see or delete another user's task.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task User_a_cannot_see_or_delete_user_b_tasks()
    {
        using var clientA = await RegisterAsync("owner.a@example.com");
        using var clientB = await RegisterAsync("owner.b@example.com");

        var created = await clientA.PostAsJsonAsync(
            "/api/todos",
            new { title = "A only", description = "Visible to A", userId = "supplied-by-the-client" },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options);
        Assert.NotNull(todo);
        Assert.Equal("A only", todo!.Title);

        var visibleToB = await clientB.GetFromJsonAsync<List<TodoResponse>>("/api/todos", TestJson.Options);
        Assert.NotNull(visibleToB);
        Assert.DoesNotContain(visibleToB, item => item.Id == todo.Id);

        var deletedByB = await clientB.DeleteAsync($"/api/todos/{todo.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deletedByB.StatusCode);

        var stillVisibleToA = await clientA.GetFromJsonAsync<List<TodoResponse>>("/api/todos", TestJson.Options);
        Assert.NotNull(stillVisibleToA);
        Assert.Contains(stillVisibleToA, item => item.Id == todo.Id && item.Title == "A only");

        var deletedByA = await clientA.DeleteAsync($"/api/todos/{todo.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deletedByA.StatusCode);

        var afterDelete = await clientA.GetFromJsonAsync<List<TodoResponse>>("/api/todos", TestJson.Options);
        Assert.NotNull(afterDelete);
        Assert.DoesNotContain(afterDelete, item => item.Id == todo.Id);
    }

    [Fact]
    /// <summary>A title with tags is stored as plain text, and a script-only title is rejected.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Task_titles_are_sanitized_and_markup_only_titles_are_rejected()
    {
        using var client = await RegisterAsync("titles@example.com");

        var created = await client.PostAsJsonAsync(
            "/api/todos",
            new { title = "  Buy <b>milk</b>  ", description = "  <i>From the shop</i>  " },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options);
        Assert.Equal("Buy milk", todo?.Title);
        Assert.Equal("From the shop", todo?.Description);

        var rejected = await client.PostAsJsonAsync(
            "/api/todos",
            new { title = "<script>alert(1)</script>", description = "A real note" },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        var body = await rejected.Content.ReadAsStringAsync();
        Assert.DoesNotContain("alert(1)", body, StringComparison.Ordinal);
    }

    [Fact]
    /// <summary>A user can rename their own task, and cannot rename someone else's.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task A_user_can_edit_their_task_and_not_someone_elses()
    {
        LogCapture.Clear();
        using var clientA = await RegisterAsync("editor.a@example.com");
        using var clientB = await RegisterAsync("editor.b@example.com");

        var created = await clientA.PostAsJsonAsync(
            "/api/todos",
            new { title = "Buy coffee", description = "From the shop" },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options);
        Assert.NotNull(todo);

        var rejected = await clientA.PutAsJsonAsync(
            $"/api/todos/{todo!.Id}",
            new { title = "<script>alert(1)</script>", description = "From the shop" },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.DoesNotContain("alert(1)", await rejected.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var edited = await clientA.PutAsJsonAsync(
            $"/api/todos/{todo.Id}",
            new { title = "  Buy <b>tea</b>  ", description = "  Loose <b>leaf</b>  " },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        var updated = await edited.Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options);
        Assert.Equal("Buy tea", updated?.Title);
        Assert.Equal("Loose leaf", updated?.Description);
        Assert.Equal(todo.CreatedAt, updated?.CreatedAt);

        var stolen = await clientB.PutAsJsonAsync(
            $"/api/todos/{todo.Id}",
            new { title = "Stolen", description = "Not yours" },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.NotFound, stolen.StatusCode);

        var visibleToA = await clientA.GetFromJsonAsync<List<TodoResponse>>("/api/todos", TestJson.Options);
        Assert.Contains(visibleToA!, item => item.Id == todo.Id && item.Title == "Buy tea");

        var log = LogCapture.Text();
        Assert.Contains("Transaction task.updated", log, StringComparison.Ordinal);
        Assert.Contains("Transaction task.update missed", log, StringComparison.Ordinal);
    }

    [Fact]
    /// <summary>Deleting a task that is not in the caller's list returns 404.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public async Task Deleting_a_missing_task_returns_404()
    {
        using var client = await RegisterAsync("missing-task@example.com");

        var response = await client.DeleteAsync($"/api/todos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("That task does not exist.", body, StringComparison.Ordinal);
    }

    /// <summary>Registers a user and returns a client that sends that user's token.</summary>
    /// <param name="email">The new account email.</param>
    /// <returns>An authorized client.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private async Task<HttpClient> RegisterAsync(string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, password = "password1", displayName = "Owner" },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options);
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }
}
