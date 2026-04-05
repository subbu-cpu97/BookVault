

using BookVault.Application.Books.Create;
using BookVault.Domain.Enums;
using FluentValidation.TestHelper;

namespace BookVault.UnitTests.Application.Books;
// TestHelper = FluentValidation's built-in test utilities
// ShouldHaveValidationErrorFor = asserts a rule fired on a property
// ShouldNotHaveValidationErrorFor = asserts a property passed validation
// Interview answer: "Why test validators separately from handlers?"
// Validators are pure functions — input in, validation result out.
// No database, no mocks needed. Test every rule in isolation.
// If you test validation inside the handler test, failures are ambiguous:
// did the handler logic fail or the validator?
public class CreateBookValidatorTests
{

    // ── Test data builder ───────────────────────────────────────────
    // Interview answer: "What is the Object Mother / Builder pattern in tests?"
    // A method that returns a valid object you can then selectively break.
    // ValidCommand() gives a good baseline — each test modifies one thing.
    // This means each test only has one reason to fail.
    private static CreateBookCommand ValidCommand() => new(
        Title: "Clean Code",
        ISBN: "9780132350884",
        Genre: Genre.Technology,
        PublishedYear: 2008,
        Price: 39.99m,
        AuthorId: Guid.NewGuid()
    );


    private readonly CreateBookValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Title_WhenEmpty_ShouldHaveValidationError(string? title)
    {
        // Arrange
        var command = ValidCommand() with { Title = title! };
        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Title is required.");

    }

    [Fact]
    public void Title_WhenExceeds300Chars_ShouldHaveValidationError()
    {
        var command = ValidCommand() with { Title = new string('A', 301) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_WhenValid_ShouldNotHaveValidationError()
    {
        var command = ValidCommand() with { Title = "Clean Code" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    // ── ISBN rules ──────────────────────────────────────────────────

    [Theory]
    [InlineData("123")]           // too short
    [InlineData("ABCDEFGHIJ")]   // letters
    [InlineData("12345678901234")] // too long (14 digits)
    [InlineData("")]
    public void ISBN_WhenInvalid_ShouldHaveValidationError(string isbn)
    {
        var command = ValidCommand() with { ISBN = isbn };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ISBN);
    }

    [Theory]
    [InlineData("9780132350884")] // valid 13-digit
    [InlineData("080442957X")]    // valid 10-digit with X check digit
    public void ISBN_WhenValid_ShouldNotHaveValidationError(string isbn)
    {
        var command = ValidCommand() with { ISBN = isbn };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ISBN);
    }

    // ── Price rules ─────────────────────────────────────────────────

    [Fact]
    public void Price_WhenNegative_ShouldHaveValidationError()
    {
        var command = ValidCommand() with { Price = -0.01m };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Price cannot be negative.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9.99)]
    [InlineData(999.99)]
    public void Price_WhenZeroOrPositive_ShouldNotHaveValidationError(decimal price)
    {
        var command = ValidCommand() with { Price = price };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    // ── AuthorId rules ──────────────────────────────────────────────

    [Fact]
    public void AuthorId_WhenEmpty_ShouldHaveValidationError()
    {
        var command = ValidCommand() with { AuthorId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AuthorId)
              .WithErrorMessage("Author is required.");
    }

    // ── Full valid command ──────────────────────────────────────────

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var command = ValidCommand();
        var result = _validator.TestValidate(command);

        // Assert zero errors — the whole command is valid
        result.ShouldNotHaveAnyValidationErrors();
    }
}
