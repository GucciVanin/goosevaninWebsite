using System.Collections.Concurrent;
using GooseWebsite.Api.Modules.Contact.Contracts;
using GooseWebsite.Api.Modules.Contact.Models;
using GooseWebsite.Api.Shared.Hosting;

namespace GooseWebsite.Api.Modules.Contact.Services;

// Holds pending contact messages until one-time email verification succeeds.
public sealed class ContactVerificationService
{
    private static readonly TimeSpan VerificationLifetime = TimeSpan.FromMinutes(30);
    private readonly ConcurrentDictionary<string, ContactMessageVerification> _pending = new();
    private readonly ILogger<ContactVerificationService> _logger;
    private readonly PublicLinkBuilder _links;

    public ContactVerificationService(ILogger<ContactVerificationService> logger, PublicLinkBuilder links)
    {
        _logger = logger;
        _links = links;
    }

    public ContactVerificationToken CreateVerification(ContactMessage message)
    {
        CleanupExpired();

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var verificationUrl = _links.FragmentLink("/contact/verify", ("token", token));
        var pendingMessage = new ContactMessageVerification(
            message.Name,
            message.Email,
            message.Reason,
            message.Message,
            DateTime.UtcNow.Add(VerificationLifetime));

        _pending[token] = pendingMessage;
        _logger.LogInformation(
            "Contact verification token created; reason={Reason}; expiresAtUtc={ExpiresAtUtc}; verificationTokenLength={VerificationTokenLength}",
            message.Reason,
            pendingMessage.ExpiresAtUtc,
            token.Length);

        return new ContactVerificationToken(token, verificationUrl);
    }

    public ContactVerificationOutcome Verify(string token)
    {
        CleanupExpired();

        if (string.IsNullOrWhiteSpace(token))
        {
            return new ContactVerificationOutcome(false, false, false, null, "The verification token is missing.");
        }

        // Removing is the claim: of two concurrent confirmations only one can take the message.
        if (!_pending.TryRemove(token, out var pending))
        {
            _logger.LogWarning("Contact verification request rejected because the token was missing, expired, or already used.");
            return new ContactVerificationOutcome(false, false, true, null, "This verification link is invalid or has already been used.");
        }

        if (pending.ExpiresAtUtc <= DateTime.UtcNow)
        {
            _logger.LogWarning("Contact verification request expired before delivery.");
            return new ContactVerificationOutcome(false, true, false, null, "This verification link has expired.");
        }

        _logger.LogInformation("Contact verification succeeded; reason={Reason}", pending.Reason);
        return new ContactVerificationOutcome(true, false, false, pending);
    }

    public void Discard(string token) => _pending.TryRemove(token, out _);

    public void Restore(string token, ContactMessageVerification pending)
    {
        if (pending.ExpiresAtUtc > DateTime.UtcNow)
        {
            _pending[token] = pending;
        }
    }

    private void CleanupExpired()
    {
        foreach (var pair in _pending)
        {
            if (pair.Value.ExpiresAtUtc <= DateTime.UtcNow)
            {
                _pending.TryRemove(pair.Key, out _);
            }
        }
    }
}
