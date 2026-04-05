using BookVault.Application.Common.Interfaces;
using MediatR;

namespace BookVault.Application.Books.GetById;

public class GetBookByIdQueryHandler(
    IBookRepository bookRepository
) : IRequestHandler<GetBookByIdQuery, BookDetailResponse?>
{
    public async Task<BookDetailResponse?> Handle(
        GetBookByIdQuery query,
        CancellationToken ct)
    {
        var book = await bookRepository.GetByIdAsync(query.Id, ct);

        if (book is null)
        {
            return null;
        }

        return new BookDetailResponse(
            book.Id,
            book.Title,
            book.ISBN,
            book.Genre.ToString(),
            book.PublishedYear,
            book.Price,
            book.AuthorId,
            book.Author?.FullName ?? "Unknown",
            book.CreatedOn,
            book.UpdatedOn
        );
    }
}
