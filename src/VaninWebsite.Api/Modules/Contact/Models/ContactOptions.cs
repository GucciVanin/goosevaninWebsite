namespace VaninWebsite.Api.Modules.Contact.Models;

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
}
