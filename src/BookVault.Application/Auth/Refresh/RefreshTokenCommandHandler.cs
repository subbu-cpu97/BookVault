using BookVault.Application.Auth.Register;
using BookVault.Application.Common.Interfaces;
using MediatR;

namespace BookVault.Application.Auth.Refresh;

public class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    IUnitOfWork unitOfWork
) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(
        RefreshTokenCommand command, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new UnauthorizedAccessException("Invalid token.");

        // Hash the incoming refresh token and compare to stored hash
        var refreshHash = tokenService.HashRefreshToken(command.RefreshToken);

        if (!user.IsRefreshTokenValid(refreshHash))
        {
            throw new UnauthorizedAccessException(
                "Refresh token is invalid or expired.");
        }

        // Issue new access token + rotate refresh token
        var newAccessToken = tokenService.GenerateAccessToken(user);
        var newRefreshToken = tokenService.GenerateRefreshToken();
        var newRefreshHash = tokenService.HashRefreshToken(newRefreshToken);

        user.SetRefreshToken(newRefreshHash, DateTime.UtcNow.AddDays(7));
        await userRepository.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new AuthResponse(
            newAccessToken,
            newRefreshToken,
            DateTime.UtcNow.AddMinutes(15),
            user.Id,
            user.Email,
            user.DisplayName
        );
    }
}
