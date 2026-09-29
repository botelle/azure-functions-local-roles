using LocalRoles.Functions.LocalDev;
using Microsoft.Extensions.Configuration;

namespace LocalRoles.Functions.Tests;

public class LocalPrincipalRegistrationTests
{
    private static readonly (string, string)[] CoreTools =
    [
        ("FUNCTIONS_CORETOOLS_ENVIRONMENT", "true"),
        ("AZURE_FUNCTIONS_ENVIRONMENT", "Development"),
    ];

    private static Func<string, string?> Env(params (string Key, string Value)[] values)
    {
        var map = values.ToDictionary(v => v.Key, v => v.Value);
        return key => map.GetValueOrDefault(key);
    }

    [Fact]
    public void Core_tools_in_development_is_local()
    {
        Assert.True(LocalPrincipalRegistration.IsLocalDevelopment(Env(CoreTools)));
    }

    [Fact]
    public void Development_without_core_tools_is_not_local()
    {
        // e.g. a dev/test AKS or Docker deployment that sets the environment name.
        Assert.False(LocalPrincipalRegistration.IsLocalDevelopment(
            Env(("AZURE_FUNCTIONS_ENVIRONMENT", "Development"))));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Core_tools_outside_development_is_not_local(string? environment)
    {
        var values = environment is null
            ? [("FUNCTIONS_CORETOOLS_ENVIRONMENT", "true")]
            : new[] { ("FUNCTIONS_CORETOOLS_ENVIRONMENT", "true"), ("AZURE_FUNCTIONS_ENVIRONMENT", environment) };

        Assert.False(LocalPrincipalRegistration.IsLocalDevelopment(Env(values)));
    }

    [Theory]
    [InlineData("WEBSITE_INSTANCE_ID")]
    [InlineData("WEBSITE_SITE_NAME")]
    [InlineData("CONTAINER_NAME")]
    [InlineData("KUBERNETES_SERVICE_HOST")]
    public void Any_hosted_marker_is_not_local(string marker)
    {
        Assert.False(LocalPrincipalRegistration.IsLocalDevelopment(Env([.. CoreTools, (marker, "anything")])));
    }

    [Fact]
    public void Gate_reads_the_process_environment_not_configuration()
    {
        // The test host is not Core Tools, so the real process environment says "not local"
        // even when configuration, which is where local.settings.json lands, says otherwise.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(CoreTools.ToDictionary(v => v.Item1, v => (string?)v.Item2))
            .Build();

        Assert.Equal("true", configuration["FUNCTIONS_CORETOOLS_ENVIRONMENT"]);
        Assert.Null(Environment.GetEnvironmentVariable("FUNCTIONS_CORETOOLS_ENVIRONMENT"));
        Assert.False(LocalPrincipalRegistration.IsLocalDevelopment());
    }
}
