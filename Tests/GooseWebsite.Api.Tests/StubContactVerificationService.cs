using GooseWebsite.Api.Modules.Contact.Contracts;
using GooseWebsite.Api.Modules.Contact.Models;
using GooseWebsite.Api.Modules.Contact.Services;

namespace GooseWebsite.Api.Tests;

// Provides predictable contact verification outcomes without sending real email.
internal sealed class StubContactVerificationService : IContactVerificationService
{
    public List<string> DiscardedTokens { get; } = [];

    public ContactVerificationToken CreateVerification(ContactMessage message)
    {
        return new ContactVerificationToken("token-123", "https://example.test/contact/verify#token=token-123");
    }

    public ContactVerificationOutcome Verify(string token)
    {
        return new ContactVerificationOutcome(true, false, false, new ContactMessageVerification("Test User", "test@example.com", "Work or collaboration", "hello", DateTime.UtcNow.AddMinutes(5)));
    }

    public void Discard(string token) => DiscardedTokens.Add(token);

    public void Restore(string token, ContactMessageVerification pending)
    {
    }
}
