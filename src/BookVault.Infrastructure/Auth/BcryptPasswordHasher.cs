using BookVault.Application.Common.Interfaces;

namespace BookVault.Infrastructure.Auth;

// BCrypt automatically generates and embeds a salt — you never handle salt manually
// Interview answer: "What is a salt in password hashing?"
// A salt is random data added to the password before hashing.
// Without salt: "password123" always hashes to the same value — rainbow tables work.
// With salt: "password123" + random salt hashes differently every time.
// BCrypt embeds the salt in the hash string itself — no separate column needed.
// Work factor (11) controls how slow the hash is — slower = harder to brute force.
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string plainText) =>
        BCrypt.Net.BCrypt.HashPassword(plainText, WorkFactor);

    public bool Verify(string plainText, string hash) =>
        BCrypt.Net.BCrypt.Verify(plainText, hash);
}
