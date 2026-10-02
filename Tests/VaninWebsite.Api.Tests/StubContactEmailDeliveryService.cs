using VaninWebsite.Api.Modules.Contact.Models;
using VaninWebsite.Api.Modules.Contact.Services;

namespace VaninWebsite.Api.Tests;

// Keeps contact feature tests isolated from external SMTP services.
internal sealed class StubContactEmailDeliveryService : IContactEmailDeliveryService
{
    public Task<bool> SendAsync(ContactMessageVerification message)
    {
        return Task.FromResult(true);
    }
}