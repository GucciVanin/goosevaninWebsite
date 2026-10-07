namespace GooseWebsite.Api.Modules.Contact.Models;

/// <summary>
/// Contact settings bound from the <c>Contact</c> configuration section. The rate-limit keys under
/// <c>Contact:RateLimit</c> are read by <c>ContactSubmissionAbuseGuard</c>.
/// </summary>
public sealed class ContactOptions
{
    public const string SectionName = "Contact";

    /// <summary>Inbox that receives verified contact messages (Gustavo's address).</summary>
    public string RecipientEmail { get; set; } = string.Empty;

    public string RecipientName { get; set; } = "Gustavo Couto Vanin";

    /// <summary>Public address of the email logo. Defaults to <c>{PublicBaseUrl}/email/gcv-logo.png</c>.</summary>
    public string LogoUrl { get; set; } = string.Empty;

    /// <summary>Ids of the templates stored at MailerSend. These are identifiers, not secrets.</summary>
    public ContactEmailTemplates Templates { get; set; } = new();
}

/// <summary>The three MailerSend templates used by the contact flow.</summary>
public sealed class ContactEmailTemplates
{
    /// <summary>Sent to the sender with the confirm link.</summary>
    public string SenderConfirmation { get; set; } = string.Empty;

    /// <summary>Sent to the sender after delivery, with a copy of their message.</summary>
    public string SenderReceipt { get; set; } = string.Empty;

    /// <summary>Sent to Gustavo with the verified message.</summary>
    public string OwnerNotification { get; set; } = string.Empty;
}
