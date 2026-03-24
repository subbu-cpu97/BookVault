using BookVault.Application.Common.Interfaces;
using MediatR;

namespace BookVault.Application.Books.GetAll;

public class GetAllBooksQueryHandler(
    IBookRepository bookRepository
) : IRequestHandler<GetAllBooksQuery, IReadOnlyList<BookSummaryResponse>>
{
    public async Task<IReadOnlyList<BookSummaryResponse>> Handle(
        GetAllBooksQuery query,
        CancellationToken ct)
    {
        var books = await bookRepository.GetAllAsync(ct);

        return books.Select(b => new BookSummaryResponse(
            b.Id,
            b.Title,
            b.ISBN,
            b.Genre.ToString(),
            b.PublishedYear,
            b.Price,
            b.Author?.FullName ?? "Unknown"
        )).ToList();
    }
}