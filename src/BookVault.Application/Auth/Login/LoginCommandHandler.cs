using BookVault.Application.Auth.Register;
using BookVault.Application.Common.Interfaces;
using MediatR;

namespace BookVault.Application.Auth.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUnitOfWork unitOfWork
) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(
        LoginCommand command, CancellationToken ct)
    {
        // 1. Find user by email
        var user = await userRepository.GetByEmailAsync(command.Email, ct);

        // IMPORTANT: same error for "user not found" and "wrong password"
        // Interview answer: "Why not say 'user not found' vs 'wrong password'?"
        // Different error messages are a user enumeration vulnerability.
        // An attacker can probe emails to discover which ones are registered.
        // Generic "Invalid credentials" reveals nothing.
        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // 2. Generate new tokens
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();
        var refreshHash = tokenService.HashRefreshToken(refreshToken);

        // 3. Rotate refresh token — invalidate old one, store new one
        // Interview answer: "What is refresh token rotation?"
        // Every login issues a new refresh token and invalidates the previous.
        // If a refresh token is stolen, it becomes useless after one legitimate use.
        user.SetRefreshToken(refreshHash, DateTime.UtcNow.AddDays(7));
        await userRepository.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(15),
            user.Id,
            user.Email,
            user.DisplayName
        );
    }
}
