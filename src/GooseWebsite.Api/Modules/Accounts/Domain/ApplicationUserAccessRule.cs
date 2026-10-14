using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Accounts.Domain;

// Users are written by the Identity managers (registration, sign-in lockout, recovery), including for anonymous
// visitors, so any caller may write; the display name is text the user typed and is limited to 100 characters.
public sealed class ApplicationUserAccessRule : EntityAccessRule<ApplicationUser>
{
    public ApplicationUserAccessRule() : base(WriteAccess.Anyone)
    {
        IsUserText(user => user.DisplayName, maxLength: 100);
    }
}
