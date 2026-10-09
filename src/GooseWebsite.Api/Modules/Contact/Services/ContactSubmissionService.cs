using System.ComponentModel.DataAnnotations;
using GooseWebsite.Api.Modules.Contact.Contracts;
using GooseWebsite.Api.Modules.Contact.Models;

namespace GooseWebsite.Api.Modules.Contact.Services;

// Owns the order of checks and the retry rules for the verification-first contact flow.
// Logs the reason and client key only: never message bodies, addresses, or tokens.
public sealed class ContactSubmissionService(
    ContactVerificationService verification,
    IContactEmailDeliveryService emailDelivery,
    ContactSubmissionAbuseGuard abuseGuard,
    ILogger<ContactSubmissionService> logger) : IContactSubmissionService
{
    private static readonly string[] AllowedReasons = ["Work or collaboration", "Personal note"];

    public async Task<ContactSubmitOutcome> SubmitAsync(ContactMessage message, string clientKey)
    {
        if (!string.IsNullOrWhiteSpace(message.Website))
        {
            logger.LogWarning("Contact submission rejected as honeypot traffic; clientKey={ClientKey}; reason={Reason}", clientKey, message.Reason);
            return new ContactSubmitOutcome(ContactSubmitStatus.Honeypot);
        }

        if (string.IsNullOrWhiteSpace(message.Name) || string.IsNullOrWhiteSpace(message.Email) || string.IsNullOrWhiteSpace(message.Reason) || string.IsNullOrWhiteSpace(message.Message))
        {
            return new ContactSubmitOutcome(ContactSubmitStatus.MissingFields);
        }

        if (!AllowedReasons.Contains(message.Reason, StringComparer.Ordinal))
        {
            return new ContactSubmitOutcome(ContactSubmitStatus.InvalidReason);
        }

        if (!new EmailAddressAttribute().IsValid(message.Email))
        {
            return new ContactSubmitOutcome(ContactSubmitStatus.InvalidEmail);
        }

        if (!abuseGuard.TryAllow(clientKey, out var rejectionReason))
        {
            logger.LogWarning("Contact submission rate limited; clientKey={ClientKey}; rejectionReason={RejectionReason}; reason={Reason}", clientKey, rejectionReason, message.Reason);
            return new ContactSubmitOutcome(ContactSubmitStatus.ClientRateLimited);
        }

        // The form emails whatever address is typed, so cap how often one address can be targeted.
        if (!abuseGuard.TryAllowRecipient(message.Email, out var recipientRejection))
        {
            logger.LogWarning("Contact submission rate limited for recipient; clientKey={ClientKey}; rejectionReason={RejectionReason}", clientKey, recipientRejection);
            return new ContactSubmitOutcome(ContactSubmitStatus.RecipientRateLimited);
        }

        // The link goes only to the submitted address; it is never returned to the caller, so
        // only someone who controls that inbox can verify it.
        var link = verification.CreateVerification(message);
        var emailed = false;
        try
        {
            emailed = await emailDelivery.SendVerificationRequestAsync(message.Name, message.Email, link.VerificationUrl);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            // An address the mail library rejects counts as a failed send; never keep the pending message.
            logger.LogWarning("Contact verification email was rejected as malformed; exceptionType={ExceptionType}", exception.GetType().Name);
        }

        if (!emailed)
        {
            verification.Discard(link.Token);
            logger.LogWarning("Contact verification email could not be sent; clientKey={ClientKey}; reason={Reason}", clientKey, message.Reason);
            return new ContactSubmitOutcome(ContactSubmitStatus.EmailNotSent);
        }

        logger.LogInformation("Contact verification request queued; clientKey={ClientKey}; reason={Reason}", clientKey, message.Reason);
        return new ContactSubmitOutcome(ContactSubmitStatus.Accepted);
    }

    public async Task<ContactVerifyOutcome> VerifyAsync(string token)
    {
        var outcome = verification.Verify(token);

        if (!outcome.IsSuccess)
        {
            if (outcome.IsExpired)
            {
                return new ContactVerifyOutcome(ContactVerifyStatus.Expired, outcome.ErrorMessage);
            }

            return outcome.IsUsed
                ? new ContactVerifyOutcome(ContactVerifyStatus.UnknownOrAlreadyUsed, outcome.ErrorMessage)
                : new ContactVerifyOutcome(ContactVerifyStatus.Invalid, outcome.ErrorMessage);
        }

        var pending = outcome.PendingMessage!;
        var delivery = await emailDelivery.SendAsync(pending);

        if (delivery != ContactDeliveryOutcome.Delivered)
        {
            // Verify consumed the token, so put the message back or the promised retry would be rejected.
            // If Gustavo was already notified, remember it so the retry only sends the receipt.
            verification.Restore(
                token,
                delivery == ContactDeliveryOutcome.OwnerNotifiedReceiptFailed ? pending with { OwnerNotified = true } : pending);
            return new ContactVerifyOutcome(ContactVerifyStatus.DeliveryFailed);
        }

        return new ContactVerifyOutcome(ContactVerifyStatus.Delivered);
    }
}
