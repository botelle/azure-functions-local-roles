namespace LocalRoles.Functions.Auth;

/// <summary>
/// Header names App Service Authentication sets on every authenticated request.
/// The platform strips client-supplied copies only while App Service Authentication
/// is enabled on the app. Anywhere else, any caller can set them.
/// </summary>
public static class ClientPrincipalHeaders
{
    public const string Principal = "X-MS-CLIENT-PRINCIPAL";
    public const string Name = "X-MS-CLIENT-PRINCIPAL-NAME";
    public const string Id = "X-MS-CLIENT-PRINCIPAL-ID";
    public const string IdentityProvider = "X-MS-CLIENT-PRINCIPAL-IDP";

    public static readonly IReadOnlyList<string> All = [Principal, Name, Id, IdentityProvider];

    /// <summary>Claim type used for roles when the payload does not name one.</summary>
    public const string DefaultRoleType = "roles";

    /// <summary>Claim type used for the display name when the payload does not name one.</summary>
    public const string DefaultNameType = "name";
}
