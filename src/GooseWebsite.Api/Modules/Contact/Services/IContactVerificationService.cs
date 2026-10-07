using GooseWebsite.Api.Modules.Contact.Contracts;
using GooseWebsite.Api.Modules.Contact.Models;

namespace GooseWebsite.Api.Modules.Contact.Services;

// Decouples contact verification endpoints from the token-store implementation.
public interface IContactVerificationService
{
    ContactVerificationToken CreateVerification(ContactMessage message);
    ContactVerificationOutcome Verify(string token);

    /// <summary>Drops a pending message, for example when its verification email could not be sent.</summary>
    void Discard(string token);

    /// <summary>Puts a verified message back after delivery failed, so the same link can be retried until it expires.</summary>
    void Restore(string token, ContactMessageVerification pending);
}
