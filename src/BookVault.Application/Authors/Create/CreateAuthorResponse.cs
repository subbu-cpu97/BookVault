namespace BookVault.Application.Authors.Create;

public record CreateAuthorResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Bio
);
