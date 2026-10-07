namespace GooseWebsite.Api.Modules.Accounts.Authorization;

/// <summary>
/// Policy names other modules use in <c>[Authorize(Policy = ...)]</c>. The API, not the Angular
/// route guards, is the authority for who may do what.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Provisioned site administrators only (private editor operations).</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Readers whose email is currently confirmed in the database (comment actions).</summary>
    public const string VerifiedReader = "VerifiedReader";
}
