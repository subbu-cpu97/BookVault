using System.Linq.Expressions;

namespace BookVault.Application.Common.Specifications;

// Abstract base — concrete specs inherit and configure via protected methods
// Interview answer: "Why abstract base instead of implementing the interface directly?"
// Reduces boilerplate. Every spec needs the same plumbing (includes list, etc).
// The base handles infrastructure. Subclasses only declare WHAT they want.
public abstract class BaseSpecification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDesc { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    // Protected helpers — subclasses call these in their constructor
    protected void AddCriteria(Expression<Func<T, bool>> criteria)
        => Criteria = criteria;

    protected void AddInclude(Expression<Func<T, object>> include)
        => Includes.Add(include);

    protected void AddOrderBy(Expression<Func<T, object>> orderBy)
        => OrderBy = orderBy;

    protected void AddOrderByDesc(Expression<Func<T, object>> orderByDesc)
        => OrderByDesc = orderByDesc;

    protected void ApplyPaging(int page, int pageSize)
    {
        Skip = (page - 1) * pageSize;
        Take = pageSize;
        IsPagingEnabled = true;
    }
}
