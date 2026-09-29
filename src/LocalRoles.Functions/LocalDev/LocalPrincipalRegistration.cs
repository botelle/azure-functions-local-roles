using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LocalRoles.Functions.LocalDev;

public static class LocalPrincipalRegistration
{
    /// <summary>
    /// Plugs in the local identity override, and does nothing anywhere else.
    /// Call after ConfigureFunctionsWebApplication so the HTTP context exists
    /// by the time the middleware runs.
    /// </summary>
    public static FunctionsApplicationBuilder UseLocalPrincipal(this FunctionsApplicationBuilder builder)
    {
        if (!IsLocalDevelopment(builder.Configuration))
        {
            return builder;
        }

        builder.Services.Configure<LocalPrincipalOptions>(
            builder.Configuration.GetSection(LocalPrincipalOptions.SectionName));
        builder.UseMiddleware<LocalPrincipalMiddleware>();
        return builder;
    }

    /// <summary>
    /// True only under Core Tools on a developer machine. Both conditions must hold:
    /// the environment says Development, and none of the variables App Service sets
    /// on a real instance are present, so a mis-set environment name in Azure is
    /// still refused.
    /// </summary>
    internal static bool IsLocalDevelopment(IConfiguration configuration) =>
        string.Equals(configuration["AZURE_FUNCTIONS_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase)
        && string.IsNullOrEmpty(configuration["WEBSITE_INSTANCE_ID"])
        && string.IsNullOrEmpty(configuration["WEBSITE_SITE_NAME"])
        && string.IsNullOrEmpty(configuration["CONTAINER_NAME"]);
}
