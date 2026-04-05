using BookVault.Domain.Entities;
using BookVault.Domain.Enums;
using BookVault.Infrastructure.Repositories;
using BookVault.IntegrationTests.Common;
using FluentAssertions;

namespace BookVault.IntegrationTests.Infrastructure;

// IClassFixture<T> = share ONE fixture instance across all tests in this class
// Interview answer: "IClassFixture vs IAsyncLifetime per test?"
// IClassFixture: one container for all tests in the class — faster, shared state risk
// IAsyncLifetime per test: fresh container per test — slowest, perfectly isolated
// IClassFixture is the right balance for repository tests — we control data carefully.

public class BookRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly BookRepositoryTestsHelper _helper;
    private readonly BookRepository _bookRepository;
    private readonly AuthorRepository _authorRepository;

    public BookRepositoryTests(DatabaseFixture fixture)
    {
        _bookRepository = new BookRepository(fixture.DbContext);
        _authorRepository = new AuthorRepository(fixture.DbContext);
        _helper = new BookRepositoryTestsHelper(fixture.DbContext);
    }

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsBook()
    {
        // Arrange — create and save an author first (FK constraint)
        var author = Author.Create("Martin", "Fowler", "Refactoring author");
        await _authorRepository.AddAsync(author);
        await _helper.SaveAsync();

        var book = Book.Create("Refactoring", "9780201485677",
            Genre.Technology, 1999, 49.99m, author.Id);

        // Act
        await _bookRepository.AddAsync(book);
        await _helper.SaveAsync();

        var found = await _bookRepository.GetByIdAsync(book.Id);

        // Assert
        found.Should().NotBeNull();
        found!.Title.Should().Be("Refactoring");
        found.ISBN.Should().Be("9780201485677");
        found.AuthorId.Should().Be(author.Id);

        // Verify audit interceptor worked — timestamps set automatically
        found.CreatedOn.Should().NotBe(default);
        found.UpdatedOn.Should().NotBe(default);
    }

    [Fact]
    public async Task GetById_WithAuthorIncluded_ReturnsBookWithAuthor()
    {
        // Arrange
        var author = Author.Create("Kent", "Beck", "TDD author");
        await _authorRepository.AddAsync(author);
        await _helper.SaveAsync();

        var book = Book.Create("TDD by Example", "9780321146533",
            Genre.Technology, 2002, 44.99m, author.Id);
        await _bookRepository.AddAsync(book);
        await _helper.SaveAsync();

        // Act
        var found = await _bookRepository.GetByIdAsync(book.Id);

        // Assert — Author is eagerly loaded
        // Interview answer: "What is eager loading in EF Core?"
        // Include(b => b.Author) adds a JOIN to the SQL query.
        // Without it, Author would be null (lazy loading is off by default).
        found!.Author.Should().NotBeNull();
        found.Author!.FirstName.Should().Be("Kent");
        found.Author.FullName.Should().Be("Kent Beck");
    }

    [Fact]
    public async Task ExistsByIsbn_WhenIsbnExists_ReturnsTrue()
    {
        // Arrange
        var author = Author.Create("Andrew", "Hunt", "Pragmatic author");
        await _authorRepository.AddAsync(author);
        await _helper.SaveAsync();

        var book = Book.Create("The Pragmatic Programmer", "9780135957059",
            Genre.Technology, 1999, 49.99m, author.Id);
        await _bookRepository.AddAsync(book);
        await _helper.SaveAsync();

        // Act
        var exists = await _bookRepository.ExistsByIsbnAsync("9780135957059");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByIsbn_WhenIsbnNotExists_ReturnsFalse()
    {
        // Act
        var exists = await _bookRepository.ExistsByIsbnAsync("0000000000000");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllBooks_OrderedByTitle()
    {
        // Arrange — clear existing and add known set
        var author = Author.Create("Test", "Author", "bio");
        await _authorRepository.AddAsync(author);
        await _helper.SaveAsync();

        var bookZ = Book.Create("Zebra Book", "9781234567891",
            Genre.Fiction, 2020, 10m, author.Id);
        var bookA = Book.Create("Apple Book", "9781234567892",
            Genre.Fiction, 2020, 10m, author.Id);

        await _bookRepository.AddAsync(bookZ);
        await _bookRepository.AddAsync(bookA);
        await _helper.SaveAsync();

        // Act
        var all = await _bookRepository.GetAllAsync();

        // Assert — verify ordering (Apple before Zebra)
        var titles = all.Select(b => b.Title).ToList();
        titles.Should().Contain("Apple Book");
        titles.IndexOf("Apple Book").Should().BeLessThan(titles.IndexOf("Zebra Book"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesBook_FromDatabase()
    {
        // Arrange
        var author = Author.Create("Delete", "Test", "bio");
        await _authorRepository.AddAsync(author);
        await _helper.SaveAsync();

        var book = Book.Create("Book To Delete", "9789999999991",
            Genre.Fiction, 2020, 5m, author.Id);
        await _bookRepository.AddAsync(book);
        await _helper.SaveAsync();

        // Act
        await _bookRepository.DeleteAsync(book);
        await _helper.SaveAsync();

        // Assert
        var found = await _bookRepository.GetByIdAsync(book.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task AuditInterceptor_SetsCreatedOnAndUpdatedOn_OnInsert()
    {
        // Arrange
        var author = Author.Create("Audit", "Test", "bio");
        await _authorRepository.AddAsync(author);
        await _helper.SaveAsync();

        var before = DateTime.UtcNow.AddSeconds(-1);

        var book = Book.Create("Audit Book", "9789999999992",
            Genre.Fiction, 2020, 5m, author.Id);

        // Act
        await _bookRepository.AddAsync(book);
        await _helper.SaveAsync();

        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert — interceptor set timestamps in the expected window
        book.CreatedOn.Should().BeAfter(before).And.BeBefore(after);
        book.UpdatedOn.Should().BeAfter(before).And.BeBefore(after);
    }
}

// Small helper to call SaveChanges without exposing DbContext publicly
internal class BookRepositoryTestsHelper(
    BookVault.Infrastructure.Persistence.BookVaultDbContext db)
{
    public async Task SaveAsync() => await db.SaveChangesAsync();
}
