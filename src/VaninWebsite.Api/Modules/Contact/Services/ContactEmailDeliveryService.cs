using System.Net.Mail;
using Microsoft.Extensions.Options;
using VaninWebsite.Api.Modules.Contact.Models;
using VaninWebsite.Api.Shared.Email;

namespace VaninWebsite.Api.Modules.Contact.Services;

// Sends the verified message to Gustavo and an acknowledgement to the sender via the shared SMTP transport.
public sealed class ContactEmailDeliveryService(
    ISmtpMailSender mailSender,
    IOptions<ContactOptions> options,
    ILogger<ContactEmailDeliveryService> logger) : IContactEmailDeliveryService
{
    public async Task<bool> SendAsync(ContactMessageVerification message)
    {
        var from = mailSender.FromAddress;
        var recipient = options.Value;
        if (from is null || string.IsNullOrWhiteSpace(recipient.RecipientEmail))
        {
            logger.LogWarning("Contact email configuration was incomplete; contact email delivery was skipped.");
            return false;
        }

        var recipientAddress = new MailAddress(recipient.RecipientEmail, recipient.RecipientName);

        using var recipientNotification = new MailMessage
        {
            From = from,
            Subject = $"New contact message from {message.Name} ({message.Email})",
            Body = $"Name: {message.Name}\nEmail: {message.Email}\nReason: {message.Reason}\n\n{message.Message}",
            IsBodyHtml = false
        };
        recipientNotification.To.Add(recipientAddress);
        recipientNotification.ReplyToList.Add(new MailAddress(message.Email, message.Name));

        using var senderAcknowledgement = new MailMessage
        {
            From = from,
            Subject = "Your message has been received",
            Body = $"Hi {message.Name},\n\nThank you for contacting Gustavo Couto Vanin. Your message has been received and is being reviewed.\n\nWe will get back to you as soon as possible.\n\nKind regards,\nGustavo Couto Vanin\n\nOriginal message:\nReason: {message.Reason}\n\n{message.Message}",
            IsBodyHtml = false
        };
        senderAcknowledgement.To.Add(new MailAddress(message.Email, message.Name));
        senderAcknowledgement.ReplyToList.Add(recipientAddress);

        // Log the reason only: never the sender's address or the message body.
        var delivered = await mailSender.SendAsync(recipientNotification, senderAcknowledgement);
        logger.LogInformation("Contact delivery finished; reason={Reason}; delivered={Delivered}.", message.Reason, delivered);
        return delivered;
    }
}
