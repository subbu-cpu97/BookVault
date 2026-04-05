
// namespace BookVault.Application.Books.GetAll;

// // Query = intent to READ state — no side effects (the Q in CQRS)
// // IRequest<T> tells MediatR what this query returns
// public record GetAllBooksQuery : IRequest<IReadOnlyList<BookSummaryResponse>>;

using BookVault.Domain.Common;
using MediatR;


namespace BookVault.Application.Books.GetAll;

// Query carries all filter/sort/page parameters from the HTTP request
// IRequest<PagedResult<BookSummaryResponse>> — returns a paged wrapper
public record GetAllBooksQuery(
    string? Search = null,
    string? Genre = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? Year = null,
    string SortBy = "title",
    bool SortDesc = false,
    int Page = 1,
    int PageSize = 10
) : IRequest<PagedResult<BookSummaryResponse>>;
