using Microsoft.AspNetCore.Identity;

namespace VaninWebsite.Api.Modules.Accounts.Domain;

// Extends identity storage with the display name consumed by application views.
public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
