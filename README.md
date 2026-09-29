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
| `Claims` | empty | Extra claims as `"type": "value"`, e.g. `{ "tid": "..." }`. |

If a request already carries any `X-MS-CLIENT-PRINCIPAL*` header, the middleware leaves it alone. You can still test one-off identities with `curl -H`.

## Why it can't switch on in Azure

The override is off everywhere except under Core Tools on a developer machine. Four separate things keep it that way:

1. **Registration is gated.** `UseLocalPrincipal()` adds the middleware only when `AZURE_FUNCTIONS_ENVIRONMENT` is `Development` and none of the variables App Service sets on a real instance (`WEBSITE_INSTANCE_ID`, `WEBSITE_SITE_NAME`, `CONTAINER_NAME`) are present. In Azure the middleware is never in the pipeline.
2. **The settings file is never deployed.** `func azure functionapp publish` and `dotnet publish` both leave out `local.settings.json`, so the `LocalPrincipal` section doesn't exist in Azure.
3. **The settings file is git-ignored.** Each developer's roles stay on their own machine, and nobody's roles get committed.
4. **Azure strips spoofed headers.** With authentication enabled, App Service removes any client-sent `X-MS-CLIENT-PRINCIPAL*` headers before the request reaches the app. So the rule that caller-supplied headers win gives nothing away in Azure.

Core Tools always sets `AZURE_FUNCTIONS_ENVIRONMENT=Development` and overrides anything in `Values`. You can't test guard 1 by changing that value. Setting `WEBSITE_SITE_NAME` in `Values` does turn the override off, and makes a good local check that the guard is wired up.

## Two things that catch people out

**`reloadOnChange` on `local.settings.json` watches the wrong file.** Core Tools runs the worker from `bin/output`, so `AddJsonFile("local.settings.json", reloadOnChange: true)` watches the copy made at build time. Your edits land on the next build, not on save. `AddLocalSettingsJson()` fixes that in Debug builds. MSBuild bakes the project directory into the assembly as `AssemblyMetadata`, and the app watches that file instead. Release builds carry no path and fall back to the relative one. It loads exactly one file, never both. If both were loaded, removing the section from one would leave a stale copy active in the other.

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
