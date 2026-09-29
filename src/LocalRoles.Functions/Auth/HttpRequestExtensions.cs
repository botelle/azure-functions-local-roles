using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace LocalRoles.Functions.Auth;

/// <summary>
/// Reads the caller's identity from the headers App Service Authentication sets.
/// This is the only code the functions use to decide who is calling. It does not
/// know or care whether the headers came from Azure or from the local override.
/// Only trustworthy behind App Service Authentication, which strips forged copies;
/// see the README.
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Returns the caller, or null when the request carries no principal headers
    /// (anonymous, or authentication not enabled).
    /// </summary>
    public static ClientPrincipal? GetClientPrincipal(this HttpRequest request)
    {
        var headers = request.Headers;
        var payload = DecodePayload(headers[ClientPrincipalHeaders.Principal].ToString());
        var name = NullIfEmpty(headers[ClientPrincipalHeaders.Name].ToString());
        var id = NullIfEmpty(headers[ClientPrincipalHeaders.Id].ToString());
        var idp = NullIfEmpty(headers[ClientPrincipalHeaders.IdentityProvider].ToString());

        if (payload is null && name is null && id is null)
        {
            return null;
        }

        // Deserialization does not enforce non-null members, so drop incomplete claims.
        var claims = (payload?.Claims ?? [])
            .Where(c => c?.Type is not null && c.Value is not null)
            .ToArray();
        var roleType = payload?.RoleType ?? ClientPrincipalHeaders.DefaultRoleType;
        var nameType = payload?.NameType ?? ClientPrincipalHeaders.DefaultNameType;

        var roles = claims
            .Where(c => string.Equals(c.Type, roleType, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        name ??= claims.FirstOrDefault(c =>
            string.Equals(c.Type, nameType, StringComparison.OrdinalIgnoreCase))?.Value;

        return new ClientPrincipal(name, id, idp ?? payload?.AuthType, roles, claims);
    }

    public static bool IsInRole(this HttpRequest request, string role) =>
        request.GetClientPrincipal()?.IsInRole(role) ?? false;

    private static ClientPrincipalPayload? DecodePayload(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            return JsonSerializer.Deserialize<ClientPrincipalPayload>(json);
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            // A malformed payload carries no claims, so it grants no roles.
            return null;
        }
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
