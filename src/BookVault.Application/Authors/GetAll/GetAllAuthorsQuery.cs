using BookVault.Domain.Common;
using MediatR;

namespace BookVault.Application.Authors.GetAll;

public record GetAllAuthorsQuery(
    string? Search = null,
    string SortBy = "lastName",
    bool SortDesc = false,
    int Page = 1,
    int PageSize = 10
) : IRequest<PagedResult<AuthorSummaryResponse>>;
