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
            .WithTags("Books")
            .WithOpenApi();

        group.MapGet("/", GetAllBooks)
            .WithName("GetAllBooks")
            .WithSummary("Get all books");

        group.MapGet("/{id:guid}", GetBookById)
            .WithName("GetBookById")
            .WithSummary("Get book by ID");

        group.MapPost("/", CreateBook)
            .WithName("CreateBook")
            .WithSummary("Create a new book");

        group.MapPut("/{id:guid}", UpdateBook)
            .WithName("UpdateBook")
            .WithSummary("Update an existing book");

        group.MapDelete("/{id:guid}", DeleteBook)
            .WithName("DeleteBook")
            .WithSummary("Delete a book");
    }

    // Each handler is a private static method — clean, testable, no controller bloat
    private static async Task<IResult> GetAllBooks(
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllBooksQuery(), ct);
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