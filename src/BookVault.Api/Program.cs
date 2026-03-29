using BookVault.Application;
using BookVault.Infrastructure;
using BookVault.Infrastructure.Persistence;
using BookVault.Api.Endpoints;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();


builder.Services.AddHealthChecks()
    .AddDbContextCheck<BookVaultDbContext>("database");

// Global exception handling — catches ValidationException + KeyNotFoundException
// so every endpoint gets consistent error responses automatically
builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<BookVault.Api.Extensions.GlobalExceptionHandler>();

var app = builder.Build();


// ─── Auto-migrate on startup ───────────────────────────────────────
// Interview answer: "Why auto-migrate in Docker but not always in production?"
// In Docker/dev: convenient — container starts, DB is always up to date.
// In production: run migrations as a separate step in your CD pipeline
// BEFORE deploying the new app version. This gives you a rollback window.
// Never auto-migrate in production — a bad migration + instant deploy = outage.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BookVaultDbContext>();
    await db.Database.MigrateAsync();  // applies any pending migrations
}


app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

app.MapOpenApi();
// app.MapScalarApiReference(options =>
// {
//     options.Title = "BookVault API";
//     options.Theme = ScalarTheme.Purple;
// });

app.MapScalarApiReference(options =>
{
    options.WithTheme(ScalarTheme.Purple);
    options.WithDefaultHttpClient(
        ScalarTarget.CSharp,
        ScalarClient.HttpClient);
});



app.MapBookEndpoints();
app.MapAuthorEndpoints();
app.MapHealthChecks("/health");

app.Run();