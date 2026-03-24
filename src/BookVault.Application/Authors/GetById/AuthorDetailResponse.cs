namespace BookVault.Application.Authors.GetById;

public record AuthorDetailResponse(
    Guid   Id,
    string FirstName,
    string LastName,
    string FullName,
    string Bio,
    DateTime CreatedOn,
    DateTime UpdatedOn,
    IReadOnlyList<AuthorBookItem> Books
);

// Nested DTO — books belonging to this author, embedded in the response
public record AuthorBookItem(
    Guid    Id,
    string  Title,
    string  Genre,
    int     PublishedYear,
    decimal Price
);