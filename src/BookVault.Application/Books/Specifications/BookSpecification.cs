using BookVault.Application.Common.Specifications;
using BookVault.Domain.Entities;
using BookVault.Domain.Enums;

namespace BookVault.Application.Books.Specifications;

// The query object — bundles all filter/sort/page params into one class
// Interview answer: "What is a Query Object pattern?"
// Instead of 6 method parameters, pass one object that carries them all.
// The specification translates it into LINQ expressions the repository applies.
public record BookQueryParameters
{
    public string? Search { get; init; }   // title or author name search
    public Genre? Genre { get; init; }   // exact genre filter
    public decimal? MinPrice { get; init; }   // price range filter
    public decimal? MaxPrice { get; init; }
    public int? Year { get; init; }   // published year filter
    public string SortBy { get; init; } = "title";
    public bool SortDesc { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class BookSpecification : BaseSpecification<Book>
{
    public BookSpecification(BookQueryParameters p)
    {
        // Always include Author — we need FullName for the response
        AddInclude(b => b.Author!);

        // ── Filtering ──────────────────────────────────────────────
        // LINQ expressions compile to SQL via EF Core — no in-memory filtering
        // Interview answer: "Why not filter in C# after fetching all rows?"
        // Fetching all rows = full table scan on every request.
        // Pushing filters to SQL means the DB does the work — indexes apply,
        // only matching rows cross the network. Always filter at the DB level.

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            // EF Core translates Contains() to SQL LIKE '%search%'
            AddCriteria(b =>
                b.Title.Contains(p.Search) ||
                (b.Author != null &&
                 (b.Author.FirstName.Contains(p.Search) ||
                  b.Author.LastName.Contains(p.Search))));
        }

        if (p.Genre.HasValue)
        {
            AddCriteria(b => b.Genre == p.Genre.Value);
        }

        if (p.MinPrice.HasValue)
        {
            AddCriteria(b => b.Price >= p.MinPrice.Value);
        }

        if (p.MaxPrice.HasValue)
        {
            AddCriteria(b => b.Price <= p.MaxPrice.Value);
        }

        if (p.Year.HasValue)
        {
            AddCriteria(b => b.PublishedYear == p.Year.Value);
        }

        // ── Sorting ────────────────────────────────────────────────
        // Interview answer: "Why not just .OrderBy(sortBy) with a string?"
        // String-based sorting requires dynamic LINQ or reflection — unsafe.
        // Explicit switch maps known strings to typed expressions — SQL-safe,
        // no injection risk, and fully supported by EF Core query translation.
        Action<BookSpecification> applySort = p.SortBy.ToLower() switch
        {
            "price" => p.SortDesc
                                 ? s => s.AddOrderByDesc(b => b.Price)
                                 : s => s.AddOrderBy(b => b.Price),
            "year" => p.SortDesc
                                 ? s => s.AddOrderByDesc(b => b.PublishedYear)
                                 : s => s.AddOrderBy(b => b.PublishedYear),
            "genre" => p.SortDesc
                                 ? s => s.AddOrderByDesc(b => b.Genre)
                                 : s => s.AddOrderBy(b => b.Genre),
            _ => p.SortDesc    // default: title
                                 ? s => s.AddOrderByDesc(b => b.Title)
                                 : s => s.AddOrderBy(b => b.Title)
        };
        applySort(this);

        // ── Pagination ─────────────────────────────────────────────
        ApplyPaging(p.Page, p.PageSize);
    }
}
