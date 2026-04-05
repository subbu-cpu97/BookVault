using BookVault.Application.Auth.Register;
using MediatR;

namespace BookVault.Application.Auth.Refresh;

public record RefreshTokenCommand(
    Guid UserId,
    string RefreshToken
) : IRequest<AuthResponse>;
