namespace LocalRoles.Functions.LocalDev;

/// <summary>
/// The identity to impersonate when running locally. Bound from the
/// "LocalPrincipal" section of local.settings.json and re-read whenever
/// the file changes.
/// </summary>
public sealed class LocalPrincipalOptions
{
    public const string SectionName = "LocalPrincipal";

    /// <summary>Set false to run as anonymous without deleting the section.</summary>
    public bool Enabled { get; set; } = true;

    public string Name { get; set; } = "local.developer@example.com";

    public string Id { get; set; } = "00000000-0000-0000-0000-000000000000";

    public string IdentityProvider { get; set; } = "aad";

    public List<string> Roles { get; set; } = [];

    /// <summary>Any extra claims to include, as claim type → value.</summary>
    public Dictionary<string, string> Claims { get; set; } = [];
}
