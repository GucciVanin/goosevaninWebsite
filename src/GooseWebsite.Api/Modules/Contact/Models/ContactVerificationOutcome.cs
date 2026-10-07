namespace GooseWebsite.Api.Modules.Contact.Models;

// Distinguishes invalid, expired, reused, and successful contact confirmations.
public sealed record ContactVerificationOutcome(bool IsValid, bool IsExpired, bool IsUsed, ContactMessageVerification? PendingMessage = null, string? ErrorMessage = null)
{
    public bool IsSuccess => IsValid && !IsExpired && !IsUsed;
}