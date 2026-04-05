

using BookVault.Domain.Entities;
using BookVault.Domain.Events;
using FluentAssertions;

namespace BookVault.UnitTests.Domain;

// [Fact] = a single test case with no parameters
// [Theory] + [InlineData] = same test logic, multiple input sets
// Interview answer: "What is the AAA pattern?"
// Arrange: set up test data and dependencies
// Act: call the method under test
// Assert: verify the result
// Every test follows this structure — makes tests readable and maintainable

public class AuthorTests
{

    [Fact]
    public void Create_WithValidInputs_ReturnsAuthorWithCorrectProperties()
    {
        // Arrange
        var firstName = "Robert";
        var lastName = "Martin";
        var bio = "Author of Clean Code";

        // Act
        var author = Author.Create(firstName, lastName, bio);

        // Assert — FluentAssertions reads like English

        author.FirstName.Should().Be(firstName);
        author.LastName.Should().Be(lastName);
        author.Bio.Should().Be(bio);
        author.FullName.Should().Be($"{firstName} {lastName}");
        author.Id.Should().NotBe(Guid.Empty); // Id should be generated
    }

    [Fact]
    public void Create_WithValidInputs_RaisesAuthorCreatedEvent()
    {
        // Arrange + Act
        var author = Author.Create("Robert", "Martin", "Clean Code author");

        // Assert
        // Interview answer: "Why test domain events?"
        // Domain events trigger downstream behavior (email, audit log).
        // If the event isn't raised, nothing downstream fires.
        // Testing it here means we catch the bug at the domain level,
        // not when an email fails to send in production.
        author.DomainEvents.Should().HaveCount(1);
        author.DomainEvents.First().Should().BeOfType<AuthorCreatedEvent>();

        var evt = (AuthorCreatedEvent)author.DomainEvents.First();
        evt.AuthorId.Should().Be(author.Id);
        evt.FullName.Should().Be("Robert Martin");

    }

    [Fact]
    public void Create_TrimsWhitespace_FromNames()
    {
        // Arrange + Act
        var author = Author.Create("  Robert  ", "  Martin  ", "bio");

        // Assert — verify trimming happens inside the entity
        author.FirstName.Should().Be("Robert");
        author.LastName.Should().Be("Martin");
    }
    // ── Validation / sad path ───────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyFirstName_ThrowsArgumentException(string? firstName)
    {
        // Act + Assert — single line for exception testing
        // Interview answer: "Why test invalid inputs?"
        // Guard clauses in constructors are easy to delete by accident.
        // Tests act as a safety net — if someone removes the guard,
        // a test breaks immediately in CI rather than silently in prod.
        var act = () => Author.Create(firstName!, "Martin", "bio");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyLastName_ThrowsArgumentException(string? lastName)
    {
        var act = () => Author.Create("Robert", lastName!, "bio");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ChangesProperties_Correctly()
    {
        // Arrange
        var author = Author.Create("Robert", "Martin", "Old bio");

        // Act
        author.Update("Uncle", "Bob", "New bio");

        // Assert
        author.FirstName.Should().Be("Uncle");
        author.LastName.Should().Be("Bob");
        author.Bio.Should().Be("New bio");
        author.FullName.Should().Be("Uncle Bob");
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var author = Author.Create("Robert", "Martin", "bio");
        author.DomainEvents.Should().HaveCount(1);

        // Act
        author.ClearDomainEvents();

        // Assert
        author.DomainEvents.Should().BeEmpty();
    }


}
