using LocalRoles.Functions.LocalDev;
using Microsoft.Extensions.Configuration;

namespace LocalRoles.Functions.Tests;

public class LocalPrincipalRegistrationTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value))
            .Build();

    [Fact]
    public void Core_tools_development_is_local()
    {
        Assert.True(LocalPrincipalRegistration.IsLocalDevelopment(
            Config(("AZURE_FUNCTIONS_ENVIRONMENT", "Development"))));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Any_other_environment_is_not_local(string? environment)
    {
        var config = environment is null ? Config() : Config(("AZURE_FUNCTIONS_ENVIRONMENT", environment));

        Assert.False(LocalPrincipalRegistration.IsLocalDevelopment(config));
    }

    [Theory]
    [InlineData("WEBSITE_INSTANCE_ID")]
    [InlineData("WEBSITE_SITE_NAME")]
    [InlineData("CONTAINER_NAME")]
    public void Development_on_an_azure_host_is_not_local(string azureVariable)
    {
        var config = Config(("AZURE_FUNCTIONS_ENVIRONMENT", "Development"), (azureVariable, "anything"));

        Assert.False(LocalPrincipalRegistration.IsLocalDevelopment(config));
    }
}
