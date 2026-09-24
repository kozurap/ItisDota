namespace ItisDota.Business.Services;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority { get; set; } = "http://localhost:8081/realms/itisdota";

    public string ServerBaseUrl { get; set; } = "http://localhost:8081";

    public string? InternalBaseUrl { get; set; }

    public string Realm { get; set; } = "itisdota";

    public string ClientId { get; set; } = "itisdota-web";

    public string ClientSecret { get; set; } = "";
}
