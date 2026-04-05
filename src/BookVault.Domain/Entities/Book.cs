
using BookVault.Domain.Enums;
using BookVault.Domain.Events;
using BookVault.Domain.Exceptions;

namespace BookVault.Domain.Entities;

public class Book : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string ISBN { get; private set; } = string.Empty;
    public Genre Genre { get; private set; }
    public int PublishedYear { get; private set; }
    public decimal Price { get; private set; }
    public Guid AuthorId { get; private set; }

    // Navigation property — EF Core populates this on Include()
    public Author? Author { get; private set; }
    private Book() { }

    public static Book Create(
       string title, string isbn, Genre genre,
       int publishedYear, decimal price, Guid authorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(isbn);

        if (price < 0)
        {
            throw new BookVaultDomainException("Price cannot be negative.");
        }

        if (publishedYear < 1000 || publishedYear > DateTime.UtcNow.Year + 1)
        {
            throw new BookVaultDomainException($"Published year {publishedYear} is not valid.");
        }

        var book = new Book
        {
            Title = title.Trim(),
            ISBN = isbn.Trim(),
            Genre = genre,
            PublishedYear = publishedYear,
            Price = price,
            AuthorId = authorId
        };

        book.RaiseDomainEvent(new BookCreatedEvent(book.Id, book.Title, book.AuthorId));
        return book;
    }

    public void Update(string title, Genre genre, decimal price)
    {
        if (price < 0)
        {
            throw new BookVaultDomainException("Price cannot be negative.");
        }

        Title = title.Trim();
        Genre = genre;
        Price = price;

        RaiseDomainEvent(new BookUpdatedEvent(Id, Title));
    }
}
