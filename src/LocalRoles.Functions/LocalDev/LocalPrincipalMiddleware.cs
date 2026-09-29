using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalRoles.Functions.LocalDev;

/// <summary>
/// Runs before every HTTP function and stamps the configured local identity onto
/// the request. Registered only for local development; see
/// <see cref="LocalPrincipalRegistration.UseLocalPrincipal"/>.
/// </summary>
internal sealed class LocalPrincipalMiddleware(
    IConfiguration configuration,
    IOptionsMonitor<LocalPrincipalOptions> options,
    ILogger<LocalPrincipalMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Checked per request so adding or removing the section takes effect on save.
        var configured = configuration.GetSection(LocalPrincipalOptions.SectionName).Exists();
        var request = context.GetHttpContext()?.Request;
        if (configured && request is not null)
        {
            var current = options.CurrentValue;
            if (LocalPrincipalHeaderWriter.TryWrite(request.Headers, current))
            {
                logger.LogWarning(
                    "LOCAL PRINCIPAL OVERRIDE: {Function} running as {Name} with roles [{Roles}]",
                    context.FunctionDefinition.Name,
                    current.Name,
                    string.Join(", ", current.Roles));
            }
        }

        return next(context);
    }
}
