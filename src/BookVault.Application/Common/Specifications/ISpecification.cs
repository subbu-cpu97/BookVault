using System.Linq.Expressions;

namespace BookVault.Application.Common.Specifications;

// Specification pattern — encapsulates a query as an object
// Interview answer: "What problem does the Specification pattern solve?"
// Without it, filtering logic leaks into every repository method.
// You end up with: GetByTitleAndGenreAndPriceRange(string, Genre, decimal, decimal)
// — combinatorial explosion. With specifications, you compose filters freely.
// One repository method: GetAllAsync(ISpecification<Book>) handles everything.
// Each specification is a standalone class — unit testable, reusable, composable.
public interface ISpecification<T>
{
    // The WHERE clause — null means no filter
    Expression<Func<T, bool>>? Criteria { get; }

    // Navigation properties to eagerly load — adds JOINs
    List<Expression<Func<T, object>>> Includes { get; }

    // ORDER BY — null means no sort
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDesc { get; }

    // Pagination
    int? Skip { get; }
    int? Take { get; }

    // When true, COUNT(*) query runs — gets total before paging
    bool IsPagingEnabled { get; }
}
