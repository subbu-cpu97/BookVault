using System.Text;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using BookVault.Api.Endpoints;
using BookVault.Application;
using BookVault.Infrastructure;
using BookVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
// ── Application Insights / Azure Monitor ──────────────────────────
// Interview answer: "What is OpenTelemetry?"
// OpenTelemetry is a vendor-neutral standard for telemetry data.
// Instead of using App Insights SDK directly (vendor lock-in),
// we use the OpenTelemetry SDK which exports to App Insights.
// Same code could export to Datadog, Jaeger, or Prometheus with one config change.
var appInsightsConnString = builder.Configuration
    ["APPLICATIONINSIGHTS_CONNECTION_STRING"];

if (!string.IsNullOrEmpty(appInsightsConnString))
{
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = appInsightsConnString;
        });
}

builder.Services.AddInfrastructure(builder.Configuration);

// ── JWT Authentication ─────────────────────────────────────────────
// Interview answer: "What does AddAuthentication vs AddAuthorization do?"
// Authentication = WHO are you? — validate the JWT, populate HttpContext.User
// Authorization  = WHAT can you do? — check if the authenticated user
//                  has permission to access the endpoint (roles, policies)
// You need both. Authentication runs first, then authorization checks the result.

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

if (string.IsNullOrEmpty(secretKey))
{
    throw new Exception("JWT SecretKey missing");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,   // rejects expired tokens
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero  // no grace period on expiry
        // Interview answer: "What is ClockSkew?"
        // By default ASP.NET adds 5 minutes grace after token expiry.
        // Setting to Zero means tokens expire exactly when the exp claim says.
        // Important for short-lived (15 min) access tokens — 5 min grace is huge.
    };
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();


// builder.Services.AddHealthChecks()
//     .AddDbContextCheck<BookVaultDbContext>("database");

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

// if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
// {
//     using var scope = app.Services.CreateScope();
//     var db = scope.ServiceProvider.GetRequiredService<BookVaultDbContext>();
//     await db.Database.MigrateAsync();  // applies any pending migrations
// }


app.UseExceptionHandler();
app.UseStatusCodePages();
// app.UseHttpsRedirection();

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
app.MapGet("/", () => "Healthy");
app.MapHealthChecks("/health");

app.Run();





// At the very bottom of Program.cs — makes Program visible to test projects
// Interview answer: "What is a partial class marker?"
// In .NET 6+, Program.cs uses top-level statements — there's no explicit
// 'public class Program'. This line makes the implicit class accessible.
public partial class Program { }
