# azure-functions-local-roles

A .NET 10 isolated Azure Functions sample that shows how to run locally as any user and any set of roles by editing `local.settings.json`, without touching the code that checks roles.

## The problem

In Azure, App Service Authentication ("Easy Auth") signs the user in and adds headers to every request: `X-MS-CLIENT-PRINCIPAL-NAME`, `X-MS-CLIENT-PRINCIPAL-ID`, and `X-MS-CLIENT-PRINCIPAL`, a base64 JSON blob that holds the claims, roles included. The app reads those headers to decide who the caller is and what they can do.

Under `func start` there is no Easy Auth, so there are no headers. Every request is anonymous, and every role check fails.

## The approach

A worker middleware runs before each function. It writes the same headers Easy Auth would, built from a `LocalPrincipal` section in `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  },
  "LocalPrincipal": {
    "Name": "local.developer@example.com",
    "Roles": [ "Reader", "Admin" ]
  }
}
```

The functions, and the `HttpRequest` extension that reads the headers, are unchanged. They can't tell a local request from a real one, so the code that runs locally is the code that runs in production. Nothing fakes the parsing step.

> **Only trust these headers behind App Service Authentication.** App Service strips client-sent `X-MS-CLIENT-PRINCIPAL*` headers only while its authentication is turned on for the app. Without it, whether that's Easy Auth switched off, self-hosted Docker, Kubernetes, or Container Apps without built-in auth, any caller can send `X-MS-CLIENT-PRINCIPAL` with an `Admin` role and the reader will believe it. This sample's endpoints are `AuthorizationLevel.Anonymous` for that reason: in Azure they rely on the platform to have authenticated the caller already.

Because the middleware sits in front of every function, the functions never mention it. That makes it the closest thing Functions has to an AOP interceptor.

## Try it

Needs the .NET 10 SDK and Azure Functions Core Tools v4.

```bash
cp src/LocalRoles.Functions/local.settings.sample.json src/LocalRoles.Functions/local.settings.json
```

```bash
cd src/LocalRoles.Functions && func start
```

| Request | With `"Roles": ["Reader", "Admin"]` | With `"Roles": ["Reader"]` | With no `LocalPrincipal` section |
|---|---|---|---|
| `GET /api/whoami` | 200, your name and roles | 200 | 401 |
| `GET /api/reports` (needs `Admin`) | 200 | 403 | 401 |

Edit the roles and save. The next request picks them up; you don't need to restart the host.

## Settings

| Key | Default | Notes |
|---|---|---|
| `Enabled` | `true` | Set `false` to go anonymous without deleting the section. |
| `Name` | `local.developer@example.com` | Becomes `X-MS-CLIENT-PRINCIPAL-NAME` and the `name` claim. |
| `Id` | all-zero GUID | `X-MS-CLIENT-PRINCIPAL-ID`. |
| `IdentityProvider` | `aad` | `X-MS-CLIENT-PRINCIPAL-IDP` and `auth_typ`. |
| `Roles` | empty | One `roles` claim each. Empty means signed in with no roles. |
| `Claims` | empty | Extra claims as a list of `{ "Type": ..., "Value": ... }`. A list rather than a map, because configuration reads `:` in a key as a separator and would silently drop URI claim types. |

If a request already carries any `X-MS-CLIENT-PRINCIPAL*` header, the middleware leaves it alone. You can still test one-off identities with `curl -H`.

## Keeping it off outside local development

`UseLocalPrincipal()` adds the middleware only when all three of these are true of the **process environment**:

- `FUNCTIONS_CORETOOLS_ENVIRONMENT` is `true`. Core Tools sets this on the worker it launches. It's a positive signal that `func start` is running, not just the absence of Azure signals.
- `AZURE_FUNCTIONS_ENVIRONMENT` is `Development`.
- None of `WEBSITE_INSTANCE_ID`, `WEBSITE_SITE_NAME`, `CONTAINER_NAME` or `KUBERNETES_SERVICE_HOST` is set. Those cover App Service and Functions plans, Container Apps, and any Kubernetes pod.

Otherwise the middleware is never in the pipeline, whatever the configuration says. The check deliberately ignores `IConfiguration`: `local.settings.json` is itself a configuration source, and reading configuration would let keys in that file blank out the hosted markers.

Other things help too, but none of them is the guard:

- **`publish` leaves the settings file out.** The Worker SDK marks `local.settings.json` `CopyToPublishDirectory="Never"`. A plain `dotnet build` does still copy it into `bin/`, so a pipeline that ships build output rather than publish output carries it along. The gate above is what makes that harmless.
- **The settings file is git-ignored.** Each developer's roles stay on their own machine, and nobody's roles get committed.
- **Caller-supplied headers win only locally.** In Azure behind App Service Authentication, the platform has already stripped them. Anywhere else, see the warning above.

Core Tools always sets `AZURE_FUNCTIONS_ENVIRONMENT=Development` and overrides anything in `Values`, so that setting is not an off switch. Values do reach the worker as environment variables, so setting `WEBSITE_SITE_NAME` in `Values` turns the override off. That makes a good local check that the gate is wired up.

## Two things that catch people out

**`reloadOnChange` on `local.settings.json` watches the wrong file.** Core Tools runs the worker from `bin/output`, so `AddJsonFile("local.settings.json", reloadOnChange: true)` watches the copy made at build time. Your edits land on the next build, not on save. `AddLocalSettingsJson()` fixes that in Debug builds. MSBuild bakes the project directory into the assembly as `AssemblyMetadata`, and the app watches that file instead. Release builds carry no path and fall back to the relative one. The catch is that a Debug build's assembly contains your absolute project path, so don't ship Debug builds. It loads exactly one file, never both. If both were loaded, removing the section from one would leave a stale copy active in the other.

**Routes starting with `admin` are reserved** by the Functions host, which is why the role-gated endpoint here is `/api/reports`.

## Adding it to an existing app

Copy `src/LocalRoles.Functions/LocalDev/` into your app and add the `AssemblyMetadata` item from the `.csproj`. Then, in `Program.cs`:

```csharp
builder.Configuration.AddLocalSettingsJson();   // replaces AddJsonFile("local.settings.json", ...)
builder.ConfigureFunctionsWebApplication();
builder.UseLocalPrincipal();                    // after ConfigureFunctionsWebApplication
```

`LocalPrincipalHeaderWriter` writes the claims under `name_typ: "name"` and `role_typ: "roles"`. It works with any reader that honours `role_typ` and `name_typ` from the payload, as `HttpRequestExtensions` here does. If your reader hard-codes a claim type, such as the full `.../claims/role` URI Entra ID sends, change the two constants in `ClientPrincipalHeaders` to match.

## Layout

```
src/LocalRoles.Functions/
  Program.cs
  Auth/        reads the principal. This is the production path.
  LocalDev/    the local override: options, header writer, middleware, registration
  Functions/   the sample endpoints
tests/LocalRoles.Functions.Tests/
```

```bash
dotnet test
```

## License

MIT
