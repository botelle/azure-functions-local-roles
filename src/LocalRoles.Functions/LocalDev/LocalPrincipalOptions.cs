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

    /// <summary>
    /// Extra claims. A list of objects rather than a type → value map, because
    /// configuration treats ':' in a key as a separator and would silently drop
    /// URI claim types such as http://schemas.microsoft.com/identity/claims/objectidentifier.
    /// </summary>
    public List<LocalClaim> Claims { get; set; } = [];
}

public sealed class LocalClaim
{
    public string Type { get; set; } = "";

    public string Value { get; set; } = "";
}
