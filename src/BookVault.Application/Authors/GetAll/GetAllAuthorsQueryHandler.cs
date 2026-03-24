using BookVault.Application.Common.Interfaces;
using MediatR;

namespace BookVault.Application.Authors.GetAll;

public class GetAllAuthorsQueryHandler(
    IAuthorRepository authorRepository
) : IRequestHandler<GetAllAuthorsQuery, IReadOnlyList<AuthorSummaryResponse>>
{
    public async Task<IReadOnlyList<AuthorSummaryResponse>> Handle(
        GetAllAuthorsQuery query,
        CancellationToken ct)
    {
        var authors = await authorRepository.GetAllAsync(ct);

        return authors.Select(a => new AuthorSummaryResponse(
            a.Id,
            a.FullName,
            a.FirstName,
            a.LastName,
            a.Books.Count
        )).ToList();
    }
}