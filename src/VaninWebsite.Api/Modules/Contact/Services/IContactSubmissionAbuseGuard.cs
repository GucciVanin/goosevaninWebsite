namespace VaninWebsite.Api.Modules.Contact.Services;

// Lets contact submission apply abuse controls independently of their storage strategy.
public interface IContactSubmissionAbuseGuard
{
    bool TryAllow(string clientKey, out string? rejectionReason);
}