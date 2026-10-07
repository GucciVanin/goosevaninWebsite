using GooseWebsite.Api.Modules.Contact.Models;

namespace GooseWebsite.Api.Modules.Contact.Services;

// Decouples contact confirmation from the configured outbound email transport.
public interface IContactEmailDeliveryService
{
    /// <summary>Emails the verification link to the sender. The message body is never included.</summary>
    Task<bool> SendVerificationRequestAsync(string name, string email, string verificationUrl);

    /// <summary>Delivers the verified message. Skips the owner notice when <c>message.OwnerNotified</c> is set.</summary>
    Task<ContactDeliveryOutcome> SendAsync(ContactMessageVerification message);
}
