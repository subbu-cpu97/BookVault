namespace BookVault.Application.Books.GetById;

// Full detail — includes author info, used on the book detail page
public record BookDetailResponse(
    Guid    Id,
    string  Title,
    string  ISBN,
    string  Genre,
    int     PublishedYear,
    decimal Price,
    Guid    AuthorId,
    string  AuthorName,
    DateTime CreatedOn,
    DateTime UpdatedOn
);