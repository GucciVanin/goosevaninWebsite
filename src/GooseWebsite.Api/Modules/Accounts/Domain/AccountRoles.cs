namespace GooseWebsite.Api.Modules.Accounts.Domain;

/// <summary>The only roles the application assigns. Both are server-managed, never client-supplied.</summary>
public static class AccountRoles
{
    public const string Admin = "Admin";
    public const string Reader = "Reader";

    public static readonly IReadOnlyList<string> All = [Admin, Reader];
}
