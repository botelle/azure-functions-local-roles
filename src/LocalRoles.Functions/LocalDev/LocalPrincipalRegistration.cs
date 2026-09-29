using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LocalRoles.Functions.LocalDev;

public static class LocalPrincipalRegistration
{
    /// <summary>
    /// Variables set by a hosting platform. Any one of them means "not a developer
    /// machine", whatever the environment name says.
    /// </summary>
    internal static readonly IReadOnlyList<string> HostedMarkers =
    [
        "WEBSITE_INSTANCE_ID",     // App Service, Functions Premium/Dedicated/Consumption
        "WEBSITE_SITE_NAME",
        "CONTAINER_NAME",          // Functions on Azure Container Apps
        "KUBERNETES_SERVICE_HOST", // AKS, KEDA, any Kubernetes pod
    ];

    /// <summary>
    /// Plugs in the local identity override, and does nothing anywhere else.
    /// Call after ConfigureFunctionsWebApplication so the HTTP context exists
    /// by the time the middleware runs.
    /// </summary>
    public static FunctionsApplicationBuilder UseLocalPrincipal(this FunctionsApplicationBuilder builder)
    {
        if (!IsLocalDevelopment())
        {
            return builder;
        }

        builder.Services.Configure<LocalPrincipalOptions>(
            builder.Configuration.GetSection(LocalPrincipalOptions.SectionName));
        builder.UseMiddleware<LocalPrincipalMiddleware>();
        return builder;
    }

    /// <summary>
    /// True only when Core Tools launched this worker on a developer machine.
    /// </summary>
    /// <remarks>
    /// Reads the process environment, never IConfiguration. local.settings.json is a
    /// configuration source, so reading IConfiguration here would let keys in that file
    /// blank out the hosted markers.
    /// </remarks>
    internal static bool IsLocalDevelopment() => IsLocalDevelopment(Environment.GetEnvironmentVariable);

    internal static bool IsLocalDevelopment(Func<string, string?> environment) =>
        // Positive signal: Core Tools sets this on the worker it launches.
        string.Equals(environment("FUNCTIONS_CORETOOLS_ENVIRONMENT"), "true", StringComparison.OrdinalIgnoreCase)
        && string.Equals(environment("AZURE_FUNCTIONS_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase)
        && HostedMarkers.All(marker => string.IsNullOrEmpty(environment(marker)));
}
