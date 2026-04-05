using BookVault.Application.Common.Specifications;
using BookVault.Domain.Entities;

namespace BookVault.Application.Authors.Specifications;

public record AuthorQueryParameters
{
    public string? Search { get; init; }
    public string SortBy { get; init; } = "lastName";
    public bool SortDesc { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class AuthorSpecification : BaseSpecification<Author>
{
    public AuthorSpecification(AuthorQueryParameters p)
    {
        AddInclude(a => a.Books);

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            AddCriteria(a =>
                a.FirstName.Contains(p.Search) ||
                a.LastName.Contains(p.Search) ||
                a.Bio.Contains(p.Search));
        }

        Action<AuthorSpecification> applySort = p.SortBy.ToLower() switch
        {
            "firstname" => p.SortDesc
                             ? s => s.AddOrderByDesc(a => a.FirstName)
                             : s => s.AddOrderBy(a => a.FirstName),
            _ => p.SortDesc
                             ? s => s.AddOrderByDesc(a => a.LastName)
                             : s => s.AddOrderBy(a => a.LastName)
        };
        applySort(this);

        ApplyPaging(p.Page, p.PageSize);
    }
}
