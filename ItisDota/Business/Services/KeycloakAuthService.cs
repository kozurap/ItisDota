using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace ItisDota.Business.Services;

public sealed class KeycloakAuthService
{
    public const string RefreshTokenName = "refresh_token";
    public const string ExpiresAtName = "expires_at";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakTokenValidator _tokenValidator;
    private readonly IOptions<KeycloakOptions> _options;
    private readonly ILogger<KeycloakAuthService> _logger;

    public KeycloakAuthService(
        IHttpClientFactory httpClientFactory,
        KeycloakTokenValidator tokenValidator,
        IOptions<KeycloakOptions> options,
        ILogger<KeycloakAuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tokenValidator = tokenValidator;
        _options = options;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await RequestTokenAsync(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["username"] = username,
                    ["password"] = password,
                    ["scope"] = "openid"
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Keycloak login failed with status {StatusCode}", (int)response.StatusCode);
                return AuthResult.Fail(await TokenErrorMessageAsync(response, cancellationToken));
            }

            var tokens = await ReadTokensAsync(response, cancellationToken);
            return tokens is null
                ? AuthResult.Fail("Не удалось выполнить вход.")
                : AuthResult.Ok(tokens);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Keycloak login request failed");
            return AuthResult.Fail("Сервис входа недоступен. Попробуйте ещё раз.");
        }
    }

    public async Task<AuthResult> RegisterAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await RequestClientCredentialsAsync(cancellationToken);
            if (adminToken is null)
            {
                return AuthResult.Fail("Не удалось зарегистрироваться. Попробуйте ещё раз.");
            }

            var createError = await CreateUserAsync(adminToken, username, password, cancellationToken);
            if (createError is not null)
            {
                return AuthResult.Fail(createError);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Keycloak registration request failed");
            return AuthResult.Fail("Сервис регистрации недоступен. Попробуйте ещё раз.");
        }

        return await LoginAsync(username, password, cancellationToken);
    }

    public async Task SignInAsync(HttpContext httpContext, AuthTokens tokens)
    {
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = tokens.RefreshTokenExpiresAt
        };
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = RefreshTokenName, Value = tokens.RefreshToken },
            new AuthenticationToken { Name = ExpiresAtName, Value = tokens.AccessTokenExpiresAt.ToString("o", CultureInfo.InvariantCulture) }
        ]);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            tokens.Principal,
            properties);
    }

    public async Task RefreshIfNeededAsync(CookieValidatePrincipalContext context)
    {
        var expiresRaw = context.Properties.GetTokenValue(ExpiresAtName);
        if (DateTimeOffset.TryParse(expiresRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAt)
            && expiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
        {
            return;
        }

        var refreshToken = context.Properties.GetTokenValue(RefreshTokenName);
        if (string.IsNullOrEmpty(refreshToken))
        {
            await RejectAsync(context);
            return;
        }

        try
        {
            var tokens = await RefreshAsync(refreshToken, context.HttpContext.RequestAborted);
            if (tokens is null)
            {
                await RejectAsync(context);
                return;
            }

            context.ReplacePrincipal(tokens.Principal);
            context.Properties.StoreTokens(
            [
                new AuthenticationToken { Name = RefreshTokenName, Value = tokens.RefreshToken },
                new AuthenticationToken { Name = ExpiresAtName, Value = tokens.AccessTokenExpiresAt.ToString("o", CultureInfo.InvariantCulture) }
            ]);
            context.Properties.ExpiresUtc = tokens.RefreshTokenExpiresAt;
            context.ShouldRenew = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Keycloak token refresh failed");
            await RejectAsync(context);
        }
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        try
        {
            var options = _options.Value;
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenLogoutUrl(options))
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = options.ClientId,
                    ["client_secret"] = options.ClientSecret,
                    ["refresh_token"] = refreshToken
                })
            };

            using var response = await CreateClient().SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Keycloak logout returned status {StatusCode}", (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Keycloak logout request failed");
        }
    }

    private async Task<AuthTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var response = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Keycloak refresh failed with status {StatusCode}", (int)response.StatusCode);
            return null;
        }

        return await ReadTokensAsync(response, cancellationToken);
    }

    private async Task<string?> RequestClientCredentialsAsync(CancellationToken cancellationToken)
    {
        using var response = await RequestTokenAsync(
            new Dictionary<string, string> { ["grant_type"] = "client_credentials" },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Keycloak service account token failed with status {StatusCode}", (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.TryGetProperty("access_token", out var token) ? token.GetString() : null;
    }

    private async Task<string?> CreateUserAsync(
        string adminToken,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var payload = new
        {
            username,
            enabled = true,
            credentials = new[]
            {
                new { type = "password", value = password, temporary = false }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, AdminUsersUrl(options))
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        using var response = await CreateClient().SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Created)
        {
            return null;
        }

        _logger.LogInformation("Keycloak user create failed with status {StatusCode}", (int)response.StatusCode);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return await ConflictMessageAsync(response, cancellationToken);
        }

        return "Не удалось зарегистрироваться. Попробуйте ещё раз.";
    }

    private async Task<HttpResponseMessage> RequestTokenAsync(
        Dictionary<string, string> form,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        form["client_id"] = options.ClientId;
        form["client_secret"] = options.ClientSecret;

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl(options))
        {
            Content = new FormUrlEncodedContent(form)
        };

        return await CreateClient().SendAsync(request, cancellationToken);
    }

    private async Task<AuthTokens?> ReadTokensAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var accessToken = root.TryGetProperty("access_token", out var access) ? access.GetString() : null;
        var refreshToken = root.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null;
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            return null;
        }

        var accessSeconds = root.TryGetProperty("expires_in", out var expiresIn) ? expiresIn.GetInt32() : 300;
        var refreshSeconds = root.TryGetProperty("refresh_expires_in", out var refreshExpiresIn) ? refreshExpiresIn.GetInt32() : 1800;
        ClaimsPrincipal validated;
        try
        {
            validated = await _tokenValidator.ValidateAccessTokenAsync(accessToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Access token validation failed");
            return null;
        }

        var username = validated.FindFirst("preferred_username")?.Value
            ?? validated.FindFirst(ClaimTypes.Name)?.Value
            ?? "";
        var subject = validated.FindFirst("sub")?.Value ?? username;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subject),
            new(ClaimTypes.Name, username)
        };

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        var now = DateTimeOffset.UtcNow;
        return new AuthTokens(
            refreshToken,
            now.AddSeconds(Math.Max(accessSeconds, 1)),
            now.AddSeconds(Math.Max(refreshSeconds, 1)),
            principal);
    }

    private static async Task<string> TokenErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var description = document.RootElement.TryGetProperty("error_description", out var error)
                ? error.GetString()
                : null;
            if (description?.Contains("not fully set up", StringComparison.OrdinalIgnoreCase) == true)
            {
                return "Учётная запись ещё не готова к входу.";
            }
        }
        catch (JsonException)
        {
            // Keycloak error bodies are JSON; anything else stays a generic login error.
        }

        return "Неверное имя пользователя или пароль.";
    }

    private static async Task<string> ConflictMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var message = document.RootElement.TryGetProperty("errorMessage", out var error)
                ? error.GetString()
                : null;
            if (message?.Contains("user", StringComparison.OrdinalIgnoreCase) == true)
            {
                return "Пользователь с таким именем уже зарегистрирован.";
            }
        }
        catch (JsonException)
        {
            // Keycloak sometimes returns an empty conflict body.
        }

        return "Такой пользователь уже зарегистрирован.";
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient(KeycloakTokenValidator.HttpClientName);

    private static string TokenUrl(KeycloakOptions options) =>
        $"{options.ServerBaseUrl.TrimEnd('/')}/realms/{options.Realm}/protocol/openid-connect/token";

    private static string TokenLogoutUrl(KeycloakOptions options) =>
        $"{options.ServerBaseUrl.TrimEnd('/')}/realms/{options.Realm}/protocol/openid-connect/logout";

    private static string AdminUsersUrl(KeycloakOptions options) =>
        $"{options.ServerBaseUrl.TrimEnd('/')}/admin/realms/{options.Realm}/users";
}
