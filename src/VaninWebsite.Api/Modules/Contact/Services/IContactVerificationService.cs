using VaninWebsite.Api.Modules.Contact.Contracts;
using VaninWebsite.Api.Modules.Contact.Models;

namespace VaninWebsite.Api.Modules.Contact.Services;

// Decouples contact verification endpoints from the token-store implementation.
public interface IContactVerificationService
{
    ContactVerificationToken CreateVerification(ContactMessage message);
    ContactVerificationOutcome Verify(string token);
}