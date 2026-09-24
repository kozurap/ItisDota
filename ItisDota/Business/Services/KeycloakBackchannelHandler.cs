using Microsoft.Extensions.Options;

namespace ItisDota.Business.Services;

public sealed class KeycloakBackchannelHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<KeycloakOptions> _options;

    public KeycloakBackchannelHandler(IOptionsMonitor<KeycloakOptions> options)
    {
        _options = options;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        var internalBase = options.InternalBaseUrl?.Trim().TrimEnd('/');
        var publicBase = options.ServerBaseUrl.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(internalBase) && request.RequestUri is { } uri)
        {
            var current = uri.ToString();
            if (current.StartsWith(publicBase, StringComparison.OrdinalIgnoreCase))
            {
                request.RequestUri = new Uri(internalBase + current[publicBase.Length..]);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
