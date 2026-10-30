using Microsoft.Extensions.Options;
using GooseWebsite.Api.Modules.Contact.Models;
using GooseWebsite.Api.Shared.Email;

namespace GooseWebsite.Api.Modules.Contact.Services;

// Renders the contact emails (ContactEmailRenderer) and sends them as finished mail through the shared mail service.
public sealed class ContactEmailDeliveryService(
    IMailService mail,
    IOptions<ContactOptions> options,
    IConfiguration configuration,
    ILogger<ContactEmailDeliveryService> logger) : IContactEmailDeliveryService
{
    public async Task<bool> SendVerificationRequestAsync(string name, string email, string verificationUrl)
    {
        var (siteUrl, logoUrl) = SiteAddresses();

        // The message body is deliberately not an input: the confirmation email never includes it.
        var rendered = ContactEmailRenderer.Confirmation(new MailAddressee(email, name), verificationUrl, logoUrl, siteUrl);
        var delivered = (await mail.SendAsync(rendered)).Succeeded;

        logger.LogInformation("Contact verification email finished; delivered={Delivered}.", delivered);
        return delivered;
    }

    public async Task<ContactDeliveryOutcome> SendAsync(ContactMessageVerification message)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.RecipientEmail))
        {
            logger.LogWarning("Contact email configuration was incomplete; contact email delivery was skipped.");
            return ContactDeliveryOutcome.Failed;
        }

        var (siteUrl, logoUrl) = SiteAddresses();
        var sender = new MailAddressee(message.Email, ContactEmailRenderer.CleanName(message.Name));
        var owner = new MailAddressee(settings.RecipientEmail, settings.RecipientName);

        // Gustavo's notice replies to the sender; the sender's receipt replies to Gustavo. A retry after
        // a failed receipt skips the notice he already received.
        var notified = message.OwnerNotified || (await mail.SendAsync(ContactEmailRenderer.OwnerNotice(
            owner, message.Name, message.Email, message.Reason, message.Message, logoUrl, siteUrl))).Succeeded;

        if (!notified)
        {
            logger.LogInformation("Contact delivery finished; reason={Reason}; outcome={Outcome}.", message.Reason, ContactDeliveryOutcome.Failed);
            return ContactDeliveryOutcome.Failed;
        }

        var acknowledged = (await mail.SendAsync(ContactEmailRenderer.Receipt(
            sender, owner, message.Reason, message.Message, logoUrl, siteUrl))).Succeeded;

        var outcome = acknowledged ? ContactDeliveryOutcome.Delivered : ContactDeliveryOutcome.OwnerNotifiedReceiptFailed;

        // Log the reason only: never the sender's address or the message body.
        logger.LogInformation("Contact delivery finished; reason={Reason}; outcome={Outcome}.", message.Reason, outcome);
        return outcome;
    }

    private (string SiteUrl, string LogoUrl) SiteAddresses()
    {
        var siteUrl = (configuration["PublicBaseUrl"] ?? "https://localhost").TrimEnd('/');
        var logoUrl = string.IsNullOrWhiteSpace(options.Value.LogoUrl) ? $"{siteUrl}/email/gcv-logo.png" : options.Value.LogoUrl;
        return (siteUrl, logoUrl);
    }
}
