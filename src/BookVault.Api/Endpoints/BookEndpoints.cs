using BookVault.Application.Books.Create;
using BookVault.Application.Books.Delete;
using BookVault.Application.Books.GetAll;
using BookVault.Application.Books.GetById;
using BookVault.Application.Books.Update;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookVault.Api.Endpoints;

public static class BookEndpoints
{
    public static void MapBookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/books")
            .WithTags("Books");

        group.MapGet("/", GetAllBooks)
        .WithName("GetAllBooks")
        .WithSummary("Get all books — supports filtering, sorting and pagination");

        group.MapGet("/{id:guid}", GetBookById)
            .WithName("GetBookById")
            .WithSummary("Get book by ID");

        group.MapPost("/", CreateBook)
            .WithName("CreateBook")
            .WithSummary("Create a new book")
            .RequireAuthorization();

        group.MapPut("/{id:guid}", UpdateBook)
            .WithName("UpdateBook")
            .WithSummary("Update an existing book")
            .RequireAuthorization();

        group.MapDelete("/{id:guid}", DeleteBook)
            .WithName("DeleteBook")
            .WithSummary("Delete a book")
            .RequireAuthorization();
    }

    // Each handler is a private static method — clean, testable, no controller bloat
    // Replace the existing GetAllBooks private method:
    private static async Task<IResult> GetAllBooks(
        // Query string parameters — ASP.NET Core binds these automatically
        // GET /books?search=clean&genre=Technology&minPrice=10&page=2&pageSize=5
        // Interview answer: "How does ASP.NET Core Minimal API bind query params?"
        // Each parameter name matches the query string key (case-insensitive).
        // [AsParameters] on a record would also work — but individual params
        // are explicit and self-documenting in the OpenAPI schema.
        string? search = null,
        string? genre = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? year = null,
        string sortBy = "title",
        bool sortDesc = false,
        int page = 1,
        int pageSize = 10,
        IMediator mediator = default!,
        CancellationToken ct = default)
    {
        // Clamp page size — never let callers request 10,000 rows
        // Interview answer: "Why clamp pageSize server-side?"
        // A client requesting pageSize=999999 would cause a full table scan.
        // Always enforce a maximum — 50 or 100 is typical for production APIs.
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(1, page);

        var query = new GetAllBooksQuery(
            search, genre, minPrice, maxPrice, year,
            sortBy, sortDesc, page, pageSize);

        var result = await mediator.Send(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetBookById(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetBookByIdQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateBook(
        [FromBody] CreateBookCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Results.CreatedAtRoute(
            "GetBookById",
            new { id = result.Id },
            result);
    }

    private static async Task<IResult> UpdateBook(
        Guid id,
        [FromBody] UpdateBookCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        // 'with' expression — creates a new record with Id replaced
        // Records are immutable, this is how you "change" one field
        await mediator.Send(command with { Id = id }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteBook(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new DeleteBookCommand(id), ct);
        return Results.NoContent();
    }
}
