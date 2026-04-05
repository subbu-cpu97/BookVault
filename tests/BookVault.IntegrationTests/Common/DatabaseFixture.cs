using BookVault.Infrastructure.Persistence;
using BookVault.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace BookVault.IntegrationTests.Common;

// IAsyncLifetime = xUnit interface for async setup and teardown
// Interview answer: "What is TestContainers?"
// A library that starts real Docker containers programmatically in tests.
// Each test class gets a fresh Postgres container — no shared state,
// no leftover data from previous tests, full isolation.
// The container starts before tests run, stops after all tests complete.
// It uses the Docker daemon on your machine — same Docker Desktop you already have.

public class DatabaseFixture : IAsyncLifetime
{
    // PostgreSqlContainer manages the Docker container lifecycle
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:15")
        .WithImage("postgres:16-alpine")     // same version as production
        .WithDatabase("bookvault_test")
        .WithUsername("test")
        .WithPassword("test_pass")
        .Build();

    public BookVaultDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Start the container — pulls image if not cached, runs postgres
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<BookVaultDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())  // real Postgres connection string
            .AddInterceptors(new AuditInterceptor())     // include our interceptor
            .Options;

        DbContext = new BookVaultDbContext(options);

        // Apply all migrations to the test database
        // Interview answer: "Why use migrations in integration tests, not EnsureCreated()?"
        // EnsureCreated() creates the schema from the current model snapshot.
        // MigrateAsync() applies each migration in order — exactly what production does.
        // This catches migration bugs (e.g., bad SQL in a migration) before they hit prod.
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        // Dispose DbContext first, then stop and remove the container
        await DbContext.DisposeAsync();
        await _postgres.StopAsync();
    }
}
