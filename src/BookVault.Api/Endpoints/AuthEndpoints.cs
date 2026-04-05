using BookVault.Application.Auth.Login;
using BookVault.Application.Auth.Refresh;
using BookVault.Application.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookVault.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Auth");

        // These three endpoints are public — no JWT required
        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register a new user")
            .AllowAnonymous();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Login and receive JWT tokens")
            .AllowAnonymous();

        group.MapPost("/refresh", Refresh)
            .WithName("RefreshToken")
            .WithSummary("Refresh an expired access token")
            .AllowAnonymous();

        // Protected — requires valid JWT — returns the current user's info
        group.MapGet("/me", Me)
            .WithName("GetCurrentUser")
            .WithSummary("Get current authenticated user")
            .RequireAuthorization();
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterCommand command,
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginCommand command,
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> Refresh(
        [FromBody] RefreshTokenCommand command,
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Results.Ok(result);
    }

    private static IResult Me(HttpContext context)
    {
        // ClaimsPrincipal — the parsed, verified identity from the JWT
        // Interview answer: "What is ClaimsPrincipal?"
        // After JWT middleware validates the token, it populates HttpContext.User
        // with a ClaimsPrincipal containing all claims from the token payload.
        // Your endpoint reads claims without touching the database — stateless.
        var userId = context.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = context.User.FindFirst(
            System.Security.Claims.ClaimTypes.Email)?.Value;
        var displayName = context.User.FindFirst(
            System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = context.User.FindFirst(
            System.Security.Claims.ClaimTypes.Role)?.Value;

        return Results.Ok(new { userId, email, displayName, role });
    }
}
