using BookVault.Domain.Events;

namespace BookVault.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    // Never expose the raw hash — private setter, never in any DTO
    // Interview answer: "Why store a hash and not the password?"
    // If the database is breached, attackers get hashes not passwords.
    // BCrypt hashes are one-way — you cannot reverse them to get the original.
    // Each hash includes a random salt — same password hashes differently
    // each time, so rainbow table attacks don't work.
    public string PasswordHash { get; private set; } = string.Empty;

    public string Role { get; private set; } = "User";

    // Refresh token stored here — hashed, same principle as password
    public string? RefreshTokenHash { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }

    private User() { }

    public static User Create(string email, string displayName, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var user = new User
        {
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email));
        return user;
    }

    // Called after generating a new refresh token
    public void SetRefreshToken(string refreshTokenHash, DateTime expiry)
    {
        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiry = expiry;
    }

    public void RevokeRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiry = null;
    }

    public bool IsRefreshTokenValid(string refreshTokenHash) =>
        RefreshTokenHash == refreshTokenHash &&
        RefreshTokenExpiry.HasValue &&
        RefreshTokenExpiry.Value > DateTime.UtcNow;
}
