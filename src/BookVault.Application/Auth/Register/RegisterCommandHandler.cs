using BookVault.Application.Common.Interfaces;
using BookVault.Domain.Entities;
using MediatR;

namespace BookVault.Application.Auth.Register;

public class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUnitOfWork unitOfWork
) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(
        RegisterCommand command, CancellationToken ct)
    {
        // 1. Check email is not already taken
        var emailTaken = await userRepository.ExistsByEmailAsync(command.Email, ct);
        if (emailTaken)
        {
            throw new InvalidOperationException(
                $"Email '{command.Email}' is already registered.");
        }

        // 2. Hash password — never store plain text
        var passwordHash = passwordHasher.Hash(command.Password);

        // 3. Create user domain entity
        var user = User.Create(command.Email, command.DisplayName, passwordHash);

        // 4. Generate tokens
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();
        var refreshHash = tokenService.HashRefreshToken(refreshToken);

        // 5. Store hashed refresh token on user (not plain text)
        user.SetRefreshToken(refreshHash, DateTime.UtcNow.AddDays(7));

        // 6. Persist
        await userRepository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken,
            refreshToken,   // return PLAIN token to client — they need the original
            DateTime.UtcNow.AddMinutes(15),
            user.Id,
            user.Email,
            user.DisplayName
        );
    }
}
