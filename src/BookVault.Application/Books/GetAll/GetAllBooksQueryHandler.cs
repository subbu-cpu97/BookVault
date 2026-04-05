using BookVault.Application.Books.Specifications;
using BookVault.Application.Common.Interfaces;
using BookVault.Domain.Common;
using BookVault.Domain.Entities;
using BookVault.Domain.Enums;
using MediatR;

namespace BookVault.Application.Books.GetAll;

public class GetAllBooksQueryHandler(
    IBookRepository bookRepository
) : IRequestHandler<GetAllBooksQuery, PagedResult<BookSummaryResponse>>
{
    public async Task<PagedResult<BookSummaryResponse>> Handle(
        GetAllBooksQuery query, CancellationToken ct)
    {
        // Parse genre string to enum — null if not provided or invalid
        Genre? genre = null;
        if (!string.IsNullOrWhiteSpace(query.Genre) &&
            Enum.TryParse<Genre>(query.Genre, ignoreCase: true, out var parsedGenre))
        {
            genre = parsedGenre;
        }

        // Build specification from query parameters
        var spec = new BookSpecification(new BookQueryParameters
        {
            Search = query.Search,
            Genre = genre,
            MinPrice = query.MinPrice,
            MaxPrice = query.MaxPrice,
            Year = query.Year,
            SortBy = query.SortBy,
            SortDesc = query.SortDesc,
            Page = query.Page,
            PageSize = query.PageSize
        });

        // Fetch paged data from repository
        var pagedBooks = await bookRepository.GetPagedAsync(
            spec, query.Page, query.PageSize, ct);

        // Map domain entities to response DTOs
        // Interview answer: "Why map inside the handler, not the repository?"
        // The repository returns domain entities — raw data.
        // The handler knows what the caller needs — the DTO shape.
        // Repository is infrastructure; mapping is application logic.
        var mappedItems = pagedBooks.Items.Select(b => new BookSummaryResponse(
            b.Id,
            b.Title,
            b.ISBN,
            b.Genre.ToString(),
            b.PublishedYear,
            b.Price,
            b.Author?.FullName ?? "Unknown"
        )).ToList();


        return PagedResult<BookSummaryResponse>.Create(
            mappedItems,
            pagedBooks.TotalCount,
            query.Page,
            query.PageSize);
    }
}
