using MediatR;

namespace BookVault.Application.Authors.GetAll;

public record GetAllAuthorsQuery : IRequest<IReadOnlyList<AuthorSummaryResponse>>;