using BookVault.Domain.Entities;
using BookVault.Domain.Enums;
using BookVault.Domain.Events;
using BookVault.Domain.Exceptions;
using FluentAssertions;

namespace BookVault.UnitTests.Domain;

public class BookTests
{
    // Helper — avoids repeating valid inputs in every test
    // Interview answer: "What is a test fixture / helper?"
    // Shared setup that multiple tests use. Reduces duplication.
    // Rule: only share setup, never share assertions between tests.
    private static readonly Guid ValidAuthorId = Guid.NewGuid();

    private static Book CreateValidBook(
        string title = "Clean Code",
        string isbn = "9780132350884",
        Genre genre = Genre.Technology,
        int publishedYear = 2008,
        decimal price = 39.99m,
        Guid? authorId = null) =>
        Book.Create(title, isbn, genre, publishedYear, price, authorId ?? ValidAuthorId);

    // ── Happy path ──────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_ReturnsBookWithCorrectProperties()
    {
        // Act
        var book = CreateValidBook();

        // Assert
        book.Title.Should().Be("Clean Code");
        book.ISBN.Should().Be("9780132350884");
        book.Genre.Should().Be(Genre.Technology);
        book.PublishedYear.Should().Be(2008);
        book.Price.Should().Be(39.99m);
        book.AuthorId.Should().Be(ValidAuthorId);
        book.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithValidInputs_RaisesBookCreatedEvent()
    {
        // Act
        var book = CreateValidBook();

        // Assert
        book.DomainEvents.Should().HaveCount(1);
        book.DomainEvents.First().Should().BeOfType<BookCreatedEvent>();

        var evt = (BookCreatedEvent)book.DomainEvents.First();
        evt.BookId.Should().Be(book.Id);
        evt.Title.Should().Be("Clean Code");
        evt.AuthorId.Should().Be(ValidAuthorId);
    }

    // ── Price validation ────────────────────────────────────────────

    [Fact]
    public void Create_WithNegativePrice_ThrowsDomainException()
    {
        // Act
        var act = () => CreateValidBook(price: -1m);

        // Assert — verify the specific exception type AND message
        act.Should().Throw<BookVaultDomainException>()
           .WithMessage("*negative*");
        // * = wildcard — message must CONTAIN "negative"
    }

    [Fact]
    public void Create_WithZeroPrice_Succeeds()
    {
        // Free books are valid — zero price is allowed
        var act = () => CreateValidBook(price: 0m);
        act.Should().NotThrow();
    }

    // ── Year validation ─────────────────────────────────────────────

    [Theory]
    [InlineData(999)]   // too old
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(9999)]  // too far in future
    public void Create_WithInvalidPublishedYear_ThrowsDomainException(int year)
    {
        var act = () => CreateValidBook(publishedYear: year);
        act.Should().Throw<BookVaultDomainException>();
    }

    [Fact]
    public void Create_WithCurrentYear_Succeeds()
    {
        var act = () => CreateValidBook(publishedYear: DateTime.UtcNow.Year);
        act.Should().NotThrow();
    }

    // ── Title/ISBN validation ───────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyTitle_ThrowsArgumentException(string? title)
    {
        var act = () => CreateValidBook(title: title!);
        act.Should().Throw<ArgumentException>();
    }

    // ── Update ──────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidInputs_ChangesProperties()
    {
        // Arrange
        var book = CreateValidBook();

        // Act
        book.Update("Clean Architecture", Genre.Technology, 49.99m);

        // Assert
        book.Title.Should().Be("Clean Architecture");
        book.Price.Should().Be(49.99m);
    }

    [Fact]
    public void Update_RaisesBookUpdatedEvent()
    {
        // Arrange
        var book = CreateValidBook();
        book.ClearDomainEvents(); // clear the Create event

        // Act
        book.Update("New Title", Genre.Technology, 29.99m);

        // Assert — exactly one new event after update
        book.DomainEvents.Should().HaveCount(1);
        book.DomainEvents.First().Should().BeOfType<BookUpdatedEvent>();
    }

    [Fact]
    public void Update_WithNegativePrice_ThrowsDomainException()
    {
        var book = CreateValidBook();
        var act = () => book.Update("Title", Genre.Technology, -5m);
        act.Should().Throw<BookVaultDomainException>();
    }
}
