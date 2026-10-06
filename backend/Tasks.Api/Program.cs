using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tasks.Api.Data;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;
