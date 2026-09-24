using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ItisDota.Business.Services;

public sealed class KeycloakTokenValidator
{
    public const string HttpClientName = "keycloak";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<KeycloakOptions> _options;
    private readonly object _gate = new();
    private ConfigurationManager<OpenIdConnectConfiguration>? _configurationManager;

    public KeycloakTokenValidator(IHttpClientFactory httpClientFactory, IOptions<KeycloakOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public async Task<ClaimsPrincipal> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        var configuration = await GetConfigurationManager().GetConfigurationAsync(cancellationToken);
        var options = _options.Value;
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Authority.TrimEnd('/'),
            ValidateAudience = true,
            ValidAudience = options.ClientId,
            ValidateLifetime = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        return handler.ValidateToken(accessToken, parameters, out _);
    }

    private ConfigurationManager<OpenIdConnectConfiguration> GetConfigurationManager()
    {
        if (_configurationManager is not null)
        {
            return _configurationManager;
        }

        lock (_gate)
        {
            if (_configurationManager is not null)
            {
                return _configurationManager;
            }

            var authority = _options.Value.Authority.TrimEnd('/');
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var retriever = new HttpDocumentRetriever(client) { RequireHttps = false };
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{authority}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                retriever);

            return _configurationManager;
        }
    }
}
