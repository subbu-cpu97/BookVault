using BookVault.Application;
using BookVault.Infrastructure;
using BookVault.Infrastructure.Persistence;
using BookVault.Api.Endpoints;
using Scalar.AspNetCore;

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