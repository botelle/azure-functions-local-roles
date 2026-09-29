using System.Text;
using System.Text.Json;
using LocalRoles.Functions.Auth;
using Microsoft.AspNetCore.Http;

namespace LocalRoles.Functions.LocalDev;

/// <summary>
/// Writes the same headers App Service Authentication would, so the production
/// read path (<see cref="HttpRequestExtensions"/>) runs unchanged locally.
/// </summary>
internal static class LocalPrincipalHeaderWriter
{
    /// <summary>
    /// Adds the principal headers unless the request already carries any of them,
    /// so a hand-crafted request (curl -H ...) still wins over the file.
    /// Returns true when headers were written.
    /// </summary>
    public static bool TryWrite(IHeaderDictionary headers, LocalPrincipalOptions options)
    {
        if (!options.Enabled || ClientPrincipalHeaders.All.Any(headers.ContainsKey))
        {
            return false;
        }

        var claims = new List<ClientPrincipalClaim>
        {
            new(ClientPrincipalHeaders.DefaultNameType, options.Name),
        };
        claims.AddRange(options.Claims.Select(c => new ClientPrincipalClaim(c.Key, c.Value)));
        claims.AddRange(options.Roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => new ClientPrincipalClaim(ClientPrincipalHeaders.DefaultRoleType, r)));

        var payload = new ClientPrincipalPayload(
            AuthType: options.IdentityProvider,
            NameType: ClientPrincipalHeaders.DefaultNameType,
            RoleType: ClientPrincipalHeaders.DefaultRoleType,
            Claims: claims);

        headers[ClientPrincipalHeaders.Principal] =
            Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        headers[ClientPrincipalHeaders.Name] = options.Name;
        headers[ClientPrincipalHeaders.Id] = options.Id;
        headers[ClientPrincipalHeaders.IdentityProvider] = options.IdentityProvider;
        return true;
    }
}
