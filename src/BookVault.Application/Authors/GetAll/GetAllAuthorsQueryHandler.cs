using BookVault.Application.Authors.Specifications;
using BookVault.Application.Common.Interfaces;
using BookVault.Domain.Common;
using MediatR;

namespace BookVault.Application.Authors.GetAll;

public class GetAllAuthorsQueryHandler(
    IAuthorRepository authorRepository
) : IRequestHandler<GetAllAuthorsQuery, PagedResult<AuthorSummaryResponse>>
{
    public async Task<PagedResult<AuthorSummaryResponse>> Handle(
        GetAllAuthorsQuery query, CancellationToken ct)
    {
        var spec = new AuthorSpecification(new AuthorQueryParameters
        {
            Search = query.Search,
            SortBy = query.SortBy,
            SortDesc = query.SortDesc,
            Page = query.Page,
            PageSize = query.PageSize
        });

        var paged = await authorRepository.GetPagedAsync(
            spec, query.Page, query.PageSize, ct);

        var mapped = paged.Items.Select(a => new AuthorSummaryResponse(
            a.Id,
            a.FullName,
            a.FirstName,
            a.LastName,
            a.Books.Count
        )).ToList();

        return PagedResult<AuthorSummaryResponse>.Create(
            mapped, paged.TotalCount, query.Page, query.PageSize);
    }
}
