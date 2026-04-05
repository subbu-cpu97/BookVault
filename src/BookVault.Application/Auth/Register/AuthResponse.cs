namespace BookVault.Application.Auth.Register;

// Shared response for Register, Login, and Refresh
// Interview answer: "Why one response type for all auth operations?"
// All three return the same data — access token + refresh token + expiry.
// Reusing it avoids duplicating identical record definitions.
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    Guid UserId,
    string Email,
    string DisplayName
);
