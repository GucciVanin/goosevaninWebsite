using Microsoft.Extensions.Options;
using GooseWebsite.Api.Modules.Contact.Models;
using GooseWebsite.Api.Shared.Email;
using static GooseWebsite.Api.Modules.Contact.Models.ContactEmailTemplateVariables;

namespace GooseWebsite.Api.Modules.Contact.Services;

// Sends the contact emails as provider templates through the shared mail service, supplying every variable each template reads.
public sealed class ContactEmailDeliveryService(
    IMailService mail,
    IOptions<ContactOptions> options,
    IConfiguration configuration,
    ILogger<ContactEmailDeliveryService> logger) : IContactEmailDeliveryService
{
    public async Task<bool> SendVerificationRequestAsync(string name, string email, string verificationUrl)
    {
        var templates = options.Value.Templates;
        if (!HasTemplate(templates.SenderConfirmation))
        {
            return false;
        }

        // The message body is deliberately not a variable: the confirmation email never includes it.
        var variables = CommonVariables(name);
        variables[VerificationUrl] = verificationUrl;

        var delivered = (await mail.SendTemplateAsync(new TemplateMail(
            templates.SenderConfirmation,
            "Confirm your email to send your message to Gustavo",
            new MailAddressee(email, name),
            variables))).Succeeded;

        logger.LogInformation("Contact verification email finished; delivered={Delivered}.", delivered);
        return delivered;
    }

    public async Task<ContactDeliveryOutcome> SendAsync(ContactMessageVerification message)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.RecipientEmail)
            || !HasTemplate(settings.Templates.OwnerNotification)
            || !HasTemplate(settings.Templates.SenderReceipt))
        {
            logger.LogWarning("Contact email configuration was incomplete; contact email delivery was skipped.");
            return ContactDeliveryOutcome.Failed;
        }

        var variables = CommonVariables(message.Name);
        variables[SenderEmail] = message.Email;
        variables[Reason] = message.Reason;
        variables[Message] = message.Message;

        var sender = new MailAddressee(message.Email, message.Name);
        var owner = new MailAddressee(settings.RecipientEmail, settings.RecipientName);

        // Gustavo's notice replies to the sender; the sender's receipt replies to Gustavo. A retry after
        // a failed receipt skips the notice he already received.
        var notified = message.OwnerNotified || (await mail.SendTemplateAsync(new TemplateMail(
            settings.Templates.OwnerNotification,
            $"New contact message from {message.Name}",
            owner,
            variables,
            ReplyTo: sender))).Succeeded;

        if (!notified)
        {
            logger.LogInformation("Contact delivery finished; reason={Reason}; outcome={Outcome}.", message.Reason, ContactDeliveryOutcome.Failed);
            return ContactDeliveryOutcome.Failed;
        }

        var acknowledged = (await mail.SendTemplateAsync(new TemplateMail(
            settings.Templates.SenderReceipt,
            "Your message has been received",
            sender,
            variables,
            ReplyTo: owner))).Succeeded;

        var outcome = acknowledged ? ContactDeliveryOutcome.Delivered : ContactDeliveryOutcome.OwnerNotifiedReceiptFailed;

        // Log the reason only: never the sender's address or the message body.
        logger.LogInformation("Contact delivery finished; reason={Reason}; outcome={Outcome}.", message.Reason, outcome);
        return outcome;
    }

    private Dictionary<string, string> CommonVariables(string name)
    {
        var siteUrl = (configuration["PublicBaseUrl"] ?? "https://localhost").TrimEnd('/');
        var logoUrl = string.IsNullOrWhiteSpace(options.Value.LogoUrl) ? $"{siteUrl}/email/gcv-logo.png" : options.Value.LogoUrl;

        return new Dictionary<string, string>
        {
            [Name] = name,
            [SiteUrl] = siteUrl,
            [LogoUrl] = logoUrl
        };
    }

    private bool HasTemplate(string templateId)
    {
        if (!string.IsNullOrWhiteSpace(templateId))
        {
            return true;
        }

        logger.LogWarning("A contact email template id is not configured; contact email delivery was skipped.");
        return false;
    }
}
