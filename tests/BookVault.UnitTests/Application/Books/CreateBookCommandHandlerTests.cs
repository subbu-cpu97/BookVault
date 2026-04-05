using BookVault.Application.Books.Create;
using BookVault.Application.Common.Interfaces;
using BookVault.Domain.Entities;
using BookVault.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace BookVault.UnitTests.Application.Books;

// NSubstitute — creates fake implementations of interfaces
// Interview answer: "What is mocking and why do we need it?"
// Handlers depend on IBookRepository and IUnitOfWork.
// We don't want to use a real database in unit tests — slow and fragile.
// NSubstitute.For<IBookRepository>() creates a fake that:
//   - Returns whatever you tell it to return
//   - Records every call made to it
//   - Lets you verify calls were made (or not made)
// Result: handler tests run in <1ms, no DB setup needed.

public class CreateBookCommandHandlerTests
{
    // ── Dependencies (mocked) ───────────────────────────────────────
    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IAuthorRepository _authorRepository = Substitute.For<IAuthorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    // System Under Test
    private readonly CreateBookCommandHandler _handler;

    public CreateBookCommandHandlerTests()
    {
        _handler = new CreateBookCommandHandler(
            _bookRepository,
            _authorRepository,
            _unitOfWork);
    }

    // ── Happy path ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsCreateBookResponse()
    {
        // Arrange
        var command = ValidCommand();
        var author = Author.Create("Robert", "Martin", "Clean Code author");

        // Tell the mock: when GetByIdAsync is called with any Guid, return author
        _authorRepository
            .GetByIdAsync(command.AuthorId, Arg.Any<CancellationToken>())
            .Returns(author);

        // Tell the mock: ISBN is not taken
        _bookRepository
            .ExistsByIsbnAsync(command.ISBN, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — verify the response shape
        result.Should().NotBeNull();
        result.Title.Should().Be(command.Title);
        result.ISBN.Should().Be(command.ISBN);
        result.AuthorId.Should().Be(command.AuthorId);
        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsAddAndSaveChanges()
    {
        // Arrange
        var command = ValidCommand();
        var author = Author.Create("Robert", "Martin", "bio");

        _authorRepository
            .GetByIdAsync(command.AuthorId, Arg.Any<CancellationToken>())
            .Returns(author);

        _bookRepository
            .ExistsByIsbnAsync(command.ISBN, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — verify the right methods were called on mocks
        // Interview answer: "What is behavior verification?"
        // State verification: check what the result IS
        // Behavior verification: check what the code DID (which methods it called)
        // Both are important — state tells you the output, behavior tells you
        // your code interacted with dependencies correctly.
        await _bookRepository.Received(1).AddAsync(
            Arg.Any<Book>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    // ── Sad path ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenAuthorNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange — mock returns null (author doesn't exist)
        var command = ValidCommand();

        _authorRepository
            .GetByIdAsync(command.AuthorId, Arg.Any<CancellationToken>())
            .ReturnsNull();  // NSubstitute extension for returning null

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage($"*{command.AuthorId}*");
    }

    [Fact]
    public async Task Handle_WhenAuthorNotFound_NeverCallsBookRepository()
    {
        // Arrange
        var command = ValidCommand();
        _authorRepository
            .GetByIdAsync(command.AuthorId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act — ignore the exception, we're testing side effects
        try { await _handler.Handle(command, CancellationToken.None); } catch { }

        // Assert — book repo should NEVER be touched if author not found
        // Interview answer: "Why test that something was NOT called?"
        // If AddAsync is called with a bad authorId, we corrupt the DB.
        // This test guarantees the early return guard works correctly.
        await _bookRepository.DidNotReceive().AddAsync(
            Arg.Any<Book>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenIsbnAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = ValidCommand();
        var author = Author.Create("Robert", "Martin", "bio");

        _authorRepository
            .GetByIdAsync(command.AuthorId, Arg.Any<CancellationToken>())
            .Returns(author);

        // ISBN is already taken
        _bookRepository
            .ExistsByIsbnAsync(command.ISBN, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage($"*{command.ISBN}*");
    }

    [Fact]
    public async Task Handle_WhenIsbnExists_NeverSaves()
    {
        // Arrange
        var command = ValidCommand();
        var author = Author.Create("Robert", "Martin", "bio");

        _authorRepository
            .GetByIdAsync(command.AuthorId, Arg.Any<CancellationToken>())
            .Returns(author);

        _bookRepository
            .ExistsByIsbnAsync(command.ISBN, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        try { await _handler.Handle(command, CancellationToken.None); } catch { }

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Test data builder ───────────────────────────────────────────
    private static CreateBookCommand ValidCommand() => new(
        Title: "Clean Code",
        ISBN: "9780132350884",
        Genre: Genre.Technology,
        PublishedYear: 2008,
        Price: 39.99m,
        AuthorId: Guid.NewGuid()
    );
}
