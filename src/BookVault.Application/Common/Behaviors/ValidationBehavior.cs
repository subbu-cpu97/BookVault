using FluentValidation;
using MediatR;

namespace BookVault.Application.Common.Behaviors;

// Runs AFTER LoggingBehavior, BEFORE the handler
// Finds all validators for this command, runs them all,
// throws if any fail — handler never runs with invalid data
public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any())
            return await next();  // no validator registered, skip

        var context = new ValidationContext<TRequest>(request);

        // Run all validators in parallel
        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);  // caught by API error handler

        return await next();
    }
}