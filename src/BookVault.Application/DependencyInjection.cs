using BookVault.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BookVault.Application;

// Extension method pattern — adds Application services to DI container
// Called once from Program.cs — clean, discoverable
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Register all IRequestHandler<,> implementations in this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Register pipeline behaviors — order matters (Logging → Validation → Handler)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Register all AbstractValidator<> implementations in this assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}