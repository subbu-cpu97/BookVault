using BookVault.Application.Common.Specifications;
using BookVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookVault.Infrastructure.Persistence.Specifications;

// SpecificationEvaluator translates a specification into an EF Core IQueryable
// Interview answer: "Why a separate evaluator class?"
// The specification lives in Application — it has no EF Core dependency.
// The evaluator lives in Infrastructure — it knows about IQueryable and EF Core.
// This maintains Clean Architecture: Application defines WHAT, Infrastructure
// defines HOW. The spec is testable without EF Core.
public static class SpecificationEvaluator<T> where T : BaseEntity
{
    public static IQueryable<T> GetQuery(
        IQueryable<T> inputQuery,
        ISpecification<T> spec)
    {
        var query = inputQuery;

        // Apply WHERE clause
        if (spec.Criteria is not null)
        {
            query = query.Where(spec.Criteria);
        }

        // Apply all Includes (JOINs)
        // Interview answer: "What does Include() do in EF Core?"
        // Without Include, navigation properties are null — EF Core doesn't
        // load related entities unless explicitly told to. Include adds a JOIN
        // to the SQL query so the related data arrives in one round trip.
        query = spec.Includes.Aggregate(query,
            (current, include) => current.Include(include));

        // Apply ORDER BY
        if (spec.OrderBy is not null)
        {
            query = query.OrderBy(spec.OrderBy);
        }
        else if (spec.OrderByDesc is not null)
        {
            query = query.OrderByDescending(spec.OrderByDesc);
        }

        // Apply SKIP + TAKE (only after filtering and sorting)
        // Interview answer: "Why must pagination come AFTER sorting?"
        // SKIP/TAKE without ORDER BY is non-deterministic — the DB can return
        // rows in any order. Page 2 might contain rows already on page 1.
        // Always sort first, then paginate.
        if (spec.IsPagingEnabled && spec.Skip.HasValue && spec.Take.HasValue)
        {
            query = query.Skip(spec.Skip.Value).Take(spec.Take.Value);
        }

        return query;
    }
}
