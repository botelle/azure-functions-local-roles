using System.Text;
using LocalRoles.Functions.Auth;
using Microsoft.AspNetCore.Http;

namespace LocalRoles.Functions.Tests;

public class HttpRequestExtensionsTests
{
    private static HttpRequest Request(params (string Name, string Value)[] headers)
    {
        var context = new DefaultHttpContext();
        foreach (var (name, value) in headers)
        {
            context.Request.Headers[name] = value;
        }
        return context.Request;
    }

    private static string Base64(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

    [Fact]
    public void No_headers_is_anonymous()
    {
        Assert.Null(Request().GetClientPrincipal());
        Assert.False(Request().IsInRole("Admin"));
    }

    [Fact]
    public void Reads_roles_by_the_payload_role_type()
    {
        // Shape App Service sends for Entra ID: role_typ is the full claim URI, and a
        // claim merely named "roles" is not a role under that payload.
        const string roleType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        var payload = Base64($$"""
            {
              "auth_typ": "aad",
              "name_typ": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
              "role_typ": "{{roleType}}",
              "claims": [
                { "typ": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "val": "ada@example.com" },
                { "typ": "{{roleType}}", "val": "Admin" },
                { "typ": "{{roleType}}", "val": "Reader" },
                { "typ": "roles", "val": "NotARole" }
              ]
            }
            """);

        var principal = Request(
            (ClientPrincipalHeaders.Principal, payload),
            (ClientPrincipalHeaders.Id, "abc-123")).GetClientPrincipal();

        Assert.NotNull(principal);
        Assert.Equal("ada@example.com", principal.Name);
        Assert.Equal("abc-123", principal.Id);
        Assert.Equal("aad", principal.IdentityProvider);
        Assert.Equal(["Admin", "Reader"], principal.Roles);
        Assert.True(principal.IsInRole("admin"));
        Assert.False(principal.IsInRole("NotARole"));
    }

    [Fact]
    public void Name_header_wins_over_name_claim()
    {
        var payload = Base64("""{"claims":[{"typ":"name","val":"from-claim"}]}""");

        var principal = Request(
            (ClientPrincipalHeaders.Principal, payload),
            (ClientPrincipalHeaders.Name, "from-header")).GetClientPrincipal();

        Assert.Equal("from-header", principal?.Name);
    }

    [Theory]
    [InlineData("""{"claims":[null,{"typ":"roles","val":"Admin"}]}""")]
    [InlineData("""{"claims":[{"typ":null,"val":"x"},{"typ":"roles"},{"typ":"roles","val":"Admin"}]}""")]
    public void Incomplete_claims_are_skipped_not_thrown(string json)
    {
        var principal = Request((ClientPrincipalHeaders.Principal, Base64(json))).GetClientPrincipal();

        Assert.NotNull(principal);
        Assert.Equal(["Admin"], principal.Roles);
        Assert.All(principal.Claims, c => Assert.NotNull(c.Type));
    }

    [Fact]
    public void Empty_role_type_grants_no_roles()
    {
        var payload = Base64("""{"role_typ":"","claims":[{"typ":"roles","val":"Admin"}]}""");

        var principal = Request((ClientPrincipalHeaders.Principal, payload)).GetClientPrincipal();

        Assert.NotNull(principal);
        Assert.Empty(principal.Roles);
    }

    [Theory]
    [InlineData("not base64!")]
    [InlineData("eyJjbGFpbXMiOnsidHlwIjoicm9sZXMifX0=")] // {"claims":{"typ":"roles"}}
    [InlineData("bm90IGpzb24=")] // "not json"
    public void Malformed_payload_grants_no_roles(string payload)
    {
        Assert.Null(Request((ClientPrincipalHeaders.Principal, payload)).GetClientPrincipal());

        var withName = Request(
            (ClientPrincipalHeaders.Principal, payload),
            (ClientPrincipalHeaders.Name, "someone")).GetClientPrincipal();

        Assert.NotNull(withName);
        Assert.Empty(withName.Roles);
    }
}
