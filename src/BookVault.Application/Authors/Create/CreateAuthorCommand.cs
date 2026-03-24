using MediatR;

namespace BookVault.Application.Authors.Create;

public record CreateAuthorCommand(
    string FirstName,
    string LastName,
    string Bio
) : IRequest<CreateAuthorResponse>;