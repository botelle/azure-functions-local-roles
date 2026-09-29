using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace LocalRoles.Functions.LocalDev;

public static class LocalSettingsFile
{
    public const string FileName = "local.settings.json";

    /// <summary>
    /// AddJsonFile("local.settings.json", optional: true, reloadOnChange: true), except that
    /// Debug builds watch the project's file instead of the copy in bin/.
    /// </summary>
    /// <remarks>
    /// Core Tools runs the worker from bin/output, so a relative path watches the copy made
    /// at build time and edits to the project's file only land on the next build. Exactly one
    /// file is loaded, never both: with two, deleting the "LocalPrincipal" section from one
    /// would leave the stale copy in the other still active.
    /// Release builds have no project path baked in and use the relative path.
    /// </remarks>
    public static IConfigurationBuilder AddLocalSettingsJson(this IConfigurationBuilder configuration)
    {
        var projectDirectory = Assembly.GetEntryAssembly()?
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "LocalSettingsDirectory")?
            .Value;

        var path = !string.IsNullOrEmpty(projectDirectory) && File.Exists(Path.Combine(projectDirectory, FileName))
            ? Path.Combine(projectDirectory, FileName)
            : FileName;

        return configuration.AddJsonFile(path, optional: true, reloadOnChange: true);
    }
}
