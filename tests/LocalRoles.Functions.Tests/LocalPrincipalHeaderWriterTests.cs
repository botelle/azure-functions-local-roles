using LocalRoles.Functions.Auth;
using LocalRoles.Functions.LocalDev;
using Microsoft.AspNetCore.Http;

namespace LocalRoles.Functions.Tests;

public class LocalPrincipalHeaderWriterTests
{
    [Fact]
    public void Written_headers_read_back_through_the_production_path()
    {
        var request = new DefaultHttpContext().Request;
        var options = new LocalPrincipalOptions
        {
            Name = "dev@example.com",
            Id = "id-1",
            IdentityProvider = "aad",
            Roles = ["Reader", "Admin", " "],
            Claims = { ["tid"] = "tenant-1" },
        };

        Assert.True(LocalPrincipalHeaderWriter.TryWrite(request.Headers, options));

        var principal = request.GetClientPrincipal();
        Assert.NotNull(principal);
        Assert.Equal("dev@example.com", principal.Name);
        Assert.Equal("id-1", principal.Id);
        Assert.Equal("aad", principal.IdentityProvider);
        Assert.Equal(["Reader", "Admin"], principal.Roles);
        Assert.Contains(principal.Claims, c => c is { Type: "tid", Value: "tenant-1" });
    }

    [Fact]
    public void No_roles_is_authenticated_but_unprivileged()
    {
        var request = new DefaultHttpContext().Request;

        LocalPrincipalHeaderWriter.TryWrite(request.Headers, new LocalPrincipalOptions());

        var principal = request.GetClientPrincipal();
        Assert.NotNull(principal);
        Assert.Empty(principal.Roles);
    }

    [Fact]
    public void Disabled_writes_nothing()
    {
        var request = new DefaultHttpContext().Request;

        var written = LocalPrincipalHeaderWriter.TryWrite(
            request.Headers, new LocalPrincipalOptions { Enabled = false, Roles = ["Admin"] });

        Assert.False(written);
        Assert.Empty(request.Headers);
        Assert.Null(request.GetClientPrincipal());
    }

    [Theory]
    [InlineData(ClientPrincipalHeaders.Principal)]
    [InlineData(ClientPrincipalHeaders.Name)]
    [InlineData(ClientPrincipalHeaders.Id)]
    [InlineData(ClientPrincipalHeaders.IdentityProvider)]
    public void A_caller_supplied_principal_header_is_left_alone(string header)
    {
        var request = new DefaultHttpContext().Request;
        request.Headers[header] = "caller-value";

        var written = LocalPrincipalHeaderWriter.TryWrite(
            request.Headers, new LocalPrincipalOptions { Roles = ["Admin"] });

        Assert.False(written);
        Assert.Single(request.Headers);
        Assert.Equal("caller-value", request.Headers[header].ToString());
    }
}
