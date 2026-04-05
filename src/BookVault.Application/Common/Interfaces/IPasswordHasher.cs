namespace BookVault.Application.Common.Interfaces;

// Interface in Application — implementation in Infrastructure
// Application layer says "I need to hash passwords"
// It does NOT say "use BCrypt" — that's an infrastructure decision
// Interview answer: "Why abstract the password hasher?"
// In tests, swap BCrypt for a simple plaintext hasher — no slow hashing.
// In production, swap BCrypt for Argon2 without changing any handler.
public interface IPasswordHasher
{
    string Hash(string plainText);
    bool Verify(string plainText, string hash);
}
