using BookVault.Application.Common.Interfaces;
using MediatR;

namespace BookVault.Application.Authors.GetById;

public class GetAuthorByIdQueryHandler(
    IAuthorRepository authorRepository
) : IRequestHandler<GetAuthorByIdQuery, AuthorDetailResponse?>
{
    public async Task<AuthorDetailResponse?> Handle(
        GetAuthorByIdQuery query,
        CancellationToken ct)
    {
        var author = await authorRepository.GetByIdAsync(query.Id, ct);

        if (author is null) return null;

        var books = author.Books.Select(b => new AuthorBookItem(
            b.Id,
            b.Title,
            b.Genre.ToString(),
            b.PublishedYear,
            b.Price
        )).ToList();

        return new AuthorDetailResponse(
            author.Id,
            author.FirstName,
            author.LastName,
            author.FullName,
            author.Bio,
            author.CreatedOn,
            author.UpdatedOn,
            books
        );
    }
}