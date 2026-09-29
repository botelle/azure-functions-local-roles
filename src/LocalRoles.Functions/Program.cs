using LocalRoles.Functions.LocalDev;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Core Tools only turns local.settings.json "Values" into environment variables.
// Loading the file as JSON as well makes nested sections such as "LocalPrincipal"
// available. The file is never published, so in Azure this adds nothing.
// Same as AddJsonFile("local.settings.json", optional: true, reloadOnChange: true),
// but in Debug it watches the file you edit rather than the copy in bin/.
builder.Configuration.AddLocalSettingsJson();

builder.ConfigureFunctionsWebApplication();

// Local only: impersonate the identity in local.settings.json "LocalPrincipal".
builder.UseLocalPrincipal();

builder.Build().Run();
