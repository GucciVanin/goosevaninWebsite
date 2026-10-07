using System.Collections.Concurrent;
using GooseWebsite.Api.Modules.Contact.Models;
using GooseWebsite.Api.Modules.Contact.Services;

namespace GooseWebsite.Api.Tests;

// Keeps contact feature tests isolated from the mail provider and records what would be sent.
internal sealed class StubContactEmailDeliveryService : IContactEmailDeliveryService
{
    private readonly ConcurrentQueue<(string Name, string Email, string Url)> _verificationRequests = new();
    private readonly ConcurrentQueue<ContactMessageVerification> _deliveredMessages = new();
    private readonly ConcurrentQueue<bool> _ownerNotifiedSeen = new();

    public IReadOnlyCollection<(string Name, string Email, string Url)> VerificationRequests => _verificationRequests.ToArray();

    public IReadOnlyCollection<ContactMessageVerification> DeliveredMessages => _deliveredMessages.ToArray();

    /// <summary>The OwnerNotified flag of each message handed to <see cref="SendAsync"/>, in order.</summary>
    public IReadOnlyCollection<bool> OwnerNotifiedSeen => _ownerNotifiedSeen.ToArray();

    public bool VerificationShouldFail { get; set; }

    public bool VerificationShouldThrow { get; set; }

    /// <summary>Nothing reaches Gustavo.</summary>
    public bool DeliveryShouldFail { get; set; }

    /// <summary>Gustavo is notified but the sender's receipt fails.</summary>
    public bool ReceiptShouldFail { get; set; }

    public Task<bool> SendVerificationRequestAsync(string name, string email, string verificationUrl)
    {
        if (VerificationShouldThrow)
        {
            throw new FormatException("malformed address");
        }

        if (VerificationShouldFail)
        {
            return Task.FromResult(false);
        }

        _verificationRequests.Enqueue((name, email, verificationUrl));
        return Task.FromResult(true);
    }

    public Task<ContactDeliveryOutcome> SendAsync(ContactMessageVerification message)
    {
        _ownerNotifiedSeen.Enqueue(message.OwnerNotified);

        if (DeliveryShouldFail)
        {
            return Task.FromResult(ContactDeliveryOutcome.Failed);
        }

        if (ReceiptShouldFail)
        {
            return Task.FromResult(ContactDeliveryOutcome.OwnerNotifiedReceiptFailed);
        }

        _deliveredMessages.Enqueue(message);
        return Task.FromResult(ContactDeliveryOutcome.Delivered);
    }
}
