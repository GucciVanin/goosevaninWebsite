using VaninWebsite.Api.Modules.Contact.Models;

namespace VaninWebsite.Api.Modules.Contact.Services;

// Decouples contact confirmation from the configured outbound email transport.
public interface IContactEmailDeliveryService
{
    Task<bool> SendAsync(ContactMessageVerification message);
}