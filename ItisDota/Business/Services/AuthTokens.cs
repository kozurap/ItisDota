using System.Security.Claims;

namespace ItisDota.Business.Services;

public sealed record AuthTokens(
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    ClaimsPrincipal Principal);

public sealed record AuthResult(bool Succeeded, string? Error, AuthTokens? Tokens)
{
    public static AuthResult Ok(AuthTokens tokens) => new(true, null, tokens);

    public static AuthResult Fail(string error) => new(false, error, null);
}
