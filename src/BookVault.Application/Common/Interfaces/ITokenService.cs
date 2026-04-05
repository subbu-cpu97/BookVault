using BookVault.Domain.Entities;

namespace BookVault.Application.Common.Interfaces;

public interface ITokenService
{
    // Generates a signed JWT access token from user claims
    string GenerateAccessToken(User user);

    // Generates a cryptographically random refresh token (opaque string)
    string GenerateRefreshToken();

    // Hashes the refresh token before storing — same principle as passwords
    string HashRefreshToken(string refreshToken);
}
