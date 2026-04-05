namespace BookVault.Application.Books.Create;

// DTO (Data Transfer Object) — what we return to the API caller
// Never return the domain entity directly — it leaks internals
public record CreateBookResponse(
    Guid Id,
    string Title,
    string ISBN,
    string Genre,
    int PublishedYear,
    decimal Price,
    Guid AuthorId
);
