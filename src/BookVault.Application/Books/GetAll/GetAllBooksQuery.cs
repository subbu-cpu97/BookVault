using MediatR;

namespace BookVault.Application.Books.GetAll;

// Query = intent to READ state — no side effects (the Q in CQRS)
// IRequest<T> tells MediatR what this query returns
public record GetAllBooksQuery : IRequest<IReadOnlyList<BookSummaryResponse>>;