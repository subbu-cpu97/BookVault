

using BookVault.Domain.Enums;
using MediatR;

namespace BookVault.Application.Books.Create;

public record CreateBookCommand(
    string Title,
    string ISBN,
    Genre Genre,
    int PublishedYear,
    decimal Price,
    Guid AuthorId
) : IRequest<CreateBookResponse>;
