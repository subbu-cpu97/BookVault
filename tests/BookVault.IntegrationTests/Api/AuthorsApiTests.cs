using System.Net;
using System.Net.Http.Json;
using BookVault.Application.Authors.Create;
using BookVault.IntegrationTests.Common;
using FluentAssertions;

namespace BookVault.IntegrationTests.Api;

// IClassFixture = share one factory (one container) across all tests
// Collection = share across test CLASSES (for future use)

public class AuthorsApiTests : IClassFixture<BookVaultApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly BookVaultApiFactory _factory;

    public AuthorsApiTests(BookVaultApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();  // real HttpClient, no network socket
    }

    // Reset DB before each test — clean slate
    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── POST /authors ───────────────────────────────────────────────

    [Fact]
    public async Task CreateAuthor_WithValidData_Returns201WithLocation()
    {
        // Arrange
        var request = new { FirstName = "Robert", LastName = "Martin", Bio = "Clean Code" };

        // Act — real HTTP POST to your running API
        var response = await _client.PostAsJsonAsync("/authors", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadFromJsonAsync<CreateAuthorResponse>();
        body.Should().NotBeNull();
        body!.FirstName.Should().Be("Robert");
        body.LastName.Should().Be("Martin");
        body.FullName.Should().Be("Robert Martin");
        body.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateAuthor_WithEmptyFirstName_Returns400WithErrors()
    {
        // Arrange
        var request = new { FirstName = "", LastName = "Martin", Bio = "bio" };

        // Act
        var response = await _client.PostAsJsonAsync("/authors", request);

        // Assert — FluentValidation + GlobalExceptionHandler → 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("FirstName");  // error references the field
    }

    // ── GET /authors ────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAuthors_WhenNoAuthors_ReturnsEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/authors");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<List<object>>();
        body.Should().NotBeNull();
        body!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAuthors_AfterCreating_ReturnsAuthorInList()
    {
        // Arrange — create an author first
        var createRequest = new { FirstName = "Kent", LastName = "Beck", Bio = "TDD" };
        await _client.PostAsJsonAsync("/authors", createRequest);

        // Act
        var response = await _client.GetAsync("/authors");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Kent");
        body.Should().Contain("Beck");
    }

    // ── GET /authors/{id} ───────────────────────────────────────────

    [Fact]
    public async Task GetAuthorById_WithValidId_Returns200()
    {
        // Arrange — create author, get its ID from response
        var createRequest = new { FirstName = "Martin", LastName = "Fowler", Bio = "bio" };
        var createResponse = await _client.PostAsJsonAsync("/authors", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuthorResponse>();

        // Act
        var response = await _client.GetAsync($"/authors/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Martin");
        body.Should().Contain("Fowler");
    }

    [Fact]
    public async Task GetAuthorById_WithUnknownId_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/authors/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /authors/{id} ───────────────────────────────────────────

    [Fact]
    public async Task UpdateAuthor_WithValidData_Returns204()
    {
        // Arrange
        var createRequest = new { FirstName = "Old", LastName = "Name", Bio = "old bio" };
        var createResponse = await _client.PostAsJsonAsync("/authors", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuthorResponse>();

        var updateRequest = new { FirstName = "New", LastName = "Name", Bio = "new bio" };

        // Act
        var response = await _client.PutAsJsonAsync($"/authors/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
