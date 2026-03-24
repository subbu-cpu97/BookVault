using BookVault.Application.Authors.Create;
using BookVault.Application.Authors.GetAll;
using BookVault.Application.Authors.GetById;
using BookVault.Application.Authors.Update;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookVault.Api.Endpoints;

public static class AuthorEndpoints
{
    public static void MapAuthorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/authors")
            .WithTags("Authors")
            .WithOpenApi();

        group.MapGet("/", GetAllAuthors)
            .WithName("GetAllAuthors")
            .WithSummary("Get all authors");

        group.MapGet("/{id:guid}", GetAuthorById)
            .WithName("GetAuthorById")
            .WithSummary("Get author by ID with their books");

        group.MapPost("/", CreateAuthor)
            .WithName("CreateAuthor")
            .WithSummary("Create a new author");

        group.MapPut("/{id:guid}", UpdateAuthor)
            .WithName("UpdateAuthor")
            .WithSummary("Update an existing author");
    }

    private static async Task<IResult> GetAllAuthors(
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllAuthorsQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAuthorById(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAuthorByIdQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateAuthor(
        [FromBody] CreateAuthorCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Results.CreatedAtRoute(
            "GetAuthorById",
            new { id = result.Id },
            result);
    }

    private static async Task<IResult> UpdateAuthor(
        Guid id,
        [FromBody] UpdateAuthorCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return Results.NoContent();
    }
}