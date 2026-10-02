using VaninWebsite.Api.Modules.Contact.Contracts;
using VaninWebsite.Api.Modules.Contact.Models;
using VaninWebsite.Api.Modules.Contact.Services;

namespace VaninWebsite.Api.Tests;

// Provides predictable contact verification outcomes without sending real email.
internal sealed class StubContactVerificationService : IContactVerificationService
{
    public ContactVerificationToken CreateVerification(ContactMessage message)
    {
        return new ContactVerificationToken("token-123", "https://example.test/verify?token=token-123");
    }

    public ContactVerificationOutcome Verify(string token)
    {
        return new ContactVerificationOutcome(true, false, false, new ContactMessageVerification("Test User", "test@example.com", "Work or collaboration", "hello", DateTime.UtcNow.AddMinutes(5)));
    }
}