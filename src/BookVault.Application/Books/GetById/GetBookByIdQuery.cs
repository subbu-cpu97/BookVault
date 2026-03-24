using MediatR;

namespace BookVault.Application.Books.GetById;

public record GetBookByIdQuery(Guid Id) : IRequest<BookDetailResponse?>;