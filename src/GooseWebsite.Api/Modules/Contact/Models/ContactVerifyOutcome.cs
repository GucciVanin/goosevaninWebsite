namespace GooseWebsite.Api.Modules.Contact.Models;

/// <summary>What happened when a sender followed a Verification link.</summary>
public enum ContactVerifyStatus
{
    /// <summary>The message reached Gustavo and the sender received a receipt.</summary>
    Delivered,

    /// <summary>The sender is verified but delivery failed; the Pending message is kept so the same link can be retried.</summary>
    DeliveryFailed,

    Expired,

    /// <summary>The token is not known, or its message was already taken.</summary>
    UnknownOrAlreadyUsed,

    /// <summary>The token was missing.</summary>
    Invalid
}

public sealed record ContactVerifyOutcome(ContactVerifyStatus Status, string? ErrorMessage = null);
