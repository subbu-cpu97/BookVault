using BookVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace BookVault.IntegrationTests.Common;

// WebApplicationFactory<T> starts your entire ASP.NET Core app in memory
// Interview answer: "What is WebApplicationFactory?"
// It boots the real Program.cs with all middleware, DI, and routing —
// but replaces the real database with a TestContainers Postgres.
// You get an HttpClient that talks to your real API over HTTP
// without opening a network socket. Fast, real, isolated.

public class BookVaultApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:15")
        .WithImage("postgres:16-alpine")
        .WithDatabase("bookvault_api_test")
        .WithUsername("test")
        .WithPassword("test_pass")
        .Build();

    // IAsyncLifetime.InitializeAsync — starts before any test
    public async Task InitializeAsync() => await _postgres.StartAsync();

    // IAsyncLifetime.DisposeAsync — stops after all tests
    public new async Task DisposeAsync() => await _postgres.StopAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string>
            {
                ["JwtSettings:SecretKey"] = "THIS_IS_SUPER_SECRET_KEY_123456789",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:AccessTokenExpiryMinutes"] = "60"
            };

            config.AddInMemoryCollection(settings!);
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BookVaultDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<BookVaultDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    // Helper — creates a scoped DbContext for seeding test data
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookVaultDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }
}
