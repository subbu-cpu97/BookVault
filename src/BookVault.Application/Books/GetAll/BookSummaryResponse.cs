namespace BookVault.Application.Books.GetAll;

// Separate DTO for list views — lighter than full detail response
// Interview answer: "Why different DTOs for list vs detail?"
// List views show 10–100 items. Returning every field wastes bandwidth.
// GetAll returns summary (no bio, no full metadata).
// GetById returns the full record. This is the CQRS read-side advantage.
public record BookSummaryResponse(
    Guid Id,
    string Title,
    string ISBN,
    string Genre,
    int PublishedYear,
    decimal Price,
    string AuthorName
);
