namespace BookVault.Application.Authors.GetAll;

public record AuthorSummaryResponse(
    Guid Id,
    string FullName,
    string FirstName,
    string LastName,
    int BookCount
);
