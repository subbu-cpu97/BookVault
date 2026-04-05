using MediatR;

namespace BookVault.Application.Authors.Update;

public record UpdateAuthorCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Bio
) : IRequest;
