using BookVault.Domain.Enums;
using MediatR;

namespace BookVault.Application.Books.Update;

// Note: Id has a default — the API endpoint sets it from the route parameter
// using the 'with' expression: command with { Id = id }
public record UpdateBookCommand(
    Guid Id,
    string Title,
    Genre Genre,
    decimal Price
) : IRequest;
// IRequest (no generic) = command returns nothing (void)
