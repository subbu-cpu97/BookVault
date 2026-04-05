using BookVault.Application.Auth.Register;
using MediatR;

namespace BookVault.Application.Auth.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthResponse>;
