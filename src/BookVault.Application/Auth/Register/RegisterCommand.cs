using MediatR;

namespace BookVault.Application.Auth.Register;

public record RegisterCommand(
    string Email,
    string DisplayName,
    string Password
) : IRequest<AuthResponse>;
