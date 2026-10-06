using System.Text.Json;

namespace Tasks.Api.Tests;

internal static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

internal sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, UserResponse User);

internal sealed record UserResponse(string Id, string Email, string DisplayName);

internal sealed record TodoResponse(Guid Id, string Title, string Description, DateTimeOffset CreatedAt);

internal sealed record ProblemBody(string? Title, string? Detail, int? Status);
