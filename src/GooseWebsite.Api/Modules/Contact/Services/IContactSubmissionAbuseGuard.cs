namespace GooseWebsite.Api.Modules.Contact.Services;

// Lets contact submission apply abuse controls independently of their storage strategy.
public interface IContactSubmissionAbuseGuard
{
    /// <summary>Limits submissions per calling device.</summary>
    bool TryAllow(string clientKey, out string? rejectionReason);

    /// <summary>Limits verification emails per submitted address, so the form cannot be used to flood a third party.</summary>
    bool TryAllowRecipient(string recipientEmail, out string? rejectionReason);
}
