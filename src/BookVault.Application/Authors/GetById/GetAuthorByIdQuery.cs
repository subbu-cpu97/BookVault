using MediatR;

namespace BookVault.Application.Authors.GetById;

public record GetAuthorByIdQuery(Guid Id) : IRequest<AuthorDetailResponse?>;
