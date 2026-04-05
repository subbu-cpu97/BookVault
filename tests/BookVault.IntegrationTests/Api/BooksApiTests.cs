using System.Net;
using System.Net.Http.Json;
using BookVault.Application.Authors.Create;
using BookVault.Application.Books.Create;
using BookVault.Domain.Enums;
using BookVault.IntegrationTests.Common;
using FluentAssertions;

namespace BookVault.IntegrationTests.Api;

public class BooksApiTests : IClassFixture<BookVaultApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly BookVaultApiFactory _factory;

    public BooksApiTests(BookVaultApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // Helper — creates an author and returns its ID
    // Interview answer: "What is a test helper/builder?"
    // Reduces duplication in test setup. Every book test needs an author.
    // Put the HTTP call in one place — if the endpoint changes, fix one place.
    private async Task<Guid> CreateAuthorAsync()
    {
        var request = new { FirstName = "Test", LastName = "Author", Bio = "bio" };
        var response = await _client.PostAsJsonAsync("/authors", request);
        var body = await response.Content.ReadFromJsonAsync<CreateAuthorResponse>();
        return body!.Id;
    }

    // ── POST /books ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateBook_WithValidData_Returns201()
    {
        // Arrange
        var authorId = await CreateAuthorAsync();

        var request = new
        {
            Title = "Clean Code",
            ISBN = "9780132350884",
            Genre = Genre.Technology,
            PublishedYear = 2008,
            Price = 39.99,
            AuthorId = authorId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/books", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<CreateBookResponse>();
        body!.Title.Should().Be("Clean Code");
        body.AuthorId.Should().Be(authorId);
    }

    [Fact]
    public async Task CreateBook_WithDuplicateIsbn_Returns409()
    {
        // Arrange
        var authorId = await CreateAuthorAsync();

        var request = new
        {
            Title = "Clean Code",
            ISBN = "9780132350884",
            Genre = Genre.Technology,
            PublishedYear = 2008,
            Price = 39.99,
            AuthorId = authorId
        };

        // Act — create twice with same ISBN
        await _client.PostAsJsonAsync("/books", request);
        var response = await _client.PostAsJsonAsync("/books", request);

        // Assert — GlobalExceptionHandler maps InvalidOperationException → 409
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateBook_WithNonExistentAuthor_Returns404()
    {
        // Arrange — use a random GUID that doesn't exist in DB
        var request = new
        {
            Title = "Orphan Book",
            ISBN = "9780000000001",
            Genre = Genre.Fiction,
            PublishedYear = 2020,
            Price = 9.99,
            AuthorId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/books", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBook_WithNegativePrice_Returns400()
    {
        // Arrange
        var authorId = await CreateAuthorAsync();

        var request = new
        {
            Title = "Bad Book",
            ISBN = "9780000000002",
            Genre = Genre.Fiction,
            PublishedYear = 2020,
            Price = -1.00,
            AuthorId = authorId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/books", request);

        // Assert — FluentValidation catches this → 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /books ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAllBooks_ReturnsOk()
    {
        var response = await _client.GetAsync("/books");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /books/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task GetBookById_WithUnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/books/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /books/{id} ──────────────────────────────────────────

    [Fact]
    public async Task DeleteBook_WithValidId_Returns204()
    {
        // Arrange
        var authorId = await CreateAuthorAsync();
        var createRequest = new
        {
            Title = "To Delete",
            ISBN = "9780000000099",
            Genre = Genre.Fiction,
            PublishedYear = 2020,
            Price = 5.99,
            AuthorId = authorId
        };
        var createResponse = await _client.PostAsJsonAsync("/books", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBookResponse>();

        // Act
        var response = await _client.DeleteAsync($"/books/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's actually gone
        var getResponse = await _client.GetAsync($"/books/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
