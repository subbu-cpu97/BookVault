using MediatR;

namespace BookVault.Application.Books.Delete;

public record DeleteBookCommand(Guid Id) : IRequest;