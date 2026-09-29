using LocalRoles.Functions.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace LocalRoles.Functions.Functions;

/// <summary>
/// Sample endpoints. (Routes starting with "admin" are reserved by the Functions host.) Nothing here knows about the local override; they read the
/// caller exactly as they would in Azure.
/// </summary>
public sealed class IdentityFunctions
{
    public const string AdminRole = "Admin";

    [Function("WhoAmI")]
    public IActionResult WhoAmI(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "whoami")] HttpRequest req)
    {
        var principal = req.GetClientPrincipal();
        if (principal is null)
        {
            return new UnauthorizedResult();
        }

        return new OkObjectResult(new
        {
            principal.Name,
            principal.Id,
            principal.IdentityProvider,
            principal.Roles,
        });
    }

    [Function("AdminReport")]
    public IActionResult AdminReport(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "reports")] HttpRequest req)
    {
        var principal = req.GetClientPrincipal();
        if (principal is null)
        {
            return new UnauthorizedResult();
        }

        return principal.IsInRole(AdminRole)
            ? new OkObjectResult(new { message = $"Hello {principal.Name}, you are an {AdminRole}." })
            : new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}
