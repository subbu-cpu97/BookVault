using BookVault.Application.Common.Interfaces;
using BookVault.Infrastructure.Persistence;
using BookVault.Infrastructure.Persistence.Interceptors;
using BookVault.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookVault.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure( 
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register the Audit Interceptor as a singleton
        services.AddSingleton<AuditInterceptor>();

        services.AddDbContext<BookVaultDbContext>((sp, options) =>
        {

            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(
                    typeof(BookVaultDbContext).Assembly.FullName));

            // Attach our interceptor — EF Core calls it on every SaveChanges
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        // IUnitOfWork → BookVaultDbContext (same scoped instance)
        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<BookVaultDbContext>());

        // Register repositories
        services.AddScoped<IBookRepository,   BookRepository>();
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        return services;
    }
}