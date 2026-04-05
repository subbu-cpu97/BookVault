using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookVault.Application.Common.Interfaces;
using BookVault.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BookVault.Infrastructure.Auth;

// Interview answer: "Walk me through how you generate a JWT."
// 1. Build claims — key-value pairs embedded in the token payload.
// 2. Create signing credentials — HMACSHA256 with our secret key.
// 3. Create SecurityTokenDescriptor — wraps claims, expiry, issuer, audience.
// 4. JwtSecurityTokenHandler.CreateToken() serialises everything to the 3-part string.
// The result is Base64-encoded, NOT encrypted — the payload is readable by anyone.
// The SIGNATURE prevents tampering — any change to header or payload breaks it.
public class JwtTokenService(IConfiguration config) : ITokenService
{
    public string GenerateAccessToken(User user)
    {
        var jwtSettings = config.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expiryMins = int.Parse(jwtSettings["AccessTokenExpiryMinutes"]!);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims — data embedded in the token
        // Interview answer: "What is a claim?"
        // A claim is a statement about the user — name, email, role, id.
        // The server puts claims IN the token at login.
        // The server READS claims FROM the token on every request — no DB lookup needed.
        // This is what makes JWT stateless.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name,               user.DisplayName),
            new Claim(ClaimTypes.Role,               user.Role),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMins),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Cryptographically random 64 bytes → Base64 string
        // Interview answer: "Why not use a JWT as a refresh token?"
        // JWTs are self-contained — you can't invalidate them without a deny list.
        // An opaque refresh token is just a random string stored in the DB.
        // Invalidating it means deleting the DB row — instant revocation.
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        // SHA256 hash — one-way, fast (unlike BCrypt which is intentionally slow)
        // We use fast hash here because refresh tokens are already random and long.
        // BCrypt's slowness is for dictionary-attack resistance on short passwords.
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(bytes);
    }
}
