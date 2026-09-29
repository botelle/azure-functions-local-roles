using System.Text.Json.Serialization;

namespace LocalRoles.Functions.Auth;

/// <summary>
/// The identity App Service Authentication ("Easy Auth") hands to the app.
/// Built from the X-MS-CLIENT-PRINCIPAL* request headers.
/// </summary>
public sealed record ClientPrincipal(
    string? Name,
    string? Id,
    string? IdentityProvider,
    IReadOnlyList<string> Roles,
    IReadOnlyList<ClientPrincipalClaim> Claims)
{
    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}

public sealed record ClientPrincipalClaim(
    [property: JsonPropertyName("typ")] string Type,
    [property: JsonPropertyName("val")] string Value);

/// <summary>
/// Wire shape of the base64-encoded JSON in X-MS-CLIENT-PRINCIPAL.
/// </summary>
internal sealed record ClientPrincipalPayload(
    [property: JsonPropertyName("auth_typ")] string? AuthType,
    [property: JsonPropertyName("name_typ")] string? NameType,
    [property: JsonPropertyName("role_typ")] string? RoleType,
    [property: JsonPropertyName("claims")] IReadOnlyList<ClientPrincipalClaim>? Claims);
