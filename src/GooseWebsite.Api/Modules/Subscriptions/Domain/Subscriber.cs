namespace GooseWebsite.Api.Modules.Subscriptions.Domain;

public enum SubscriberState
{
    /// <summary>The address was submitted but the confirmation link has not been opened yet.</summary>
    Pending,

    /// <summary>The owner of the address confirmed it; only confirmed subscribers receive updates.</summary>
    Confirmed
}

// A person who asked to be emailed when Gustavo publishes something. The only personal data kept is the name
// and the email address; everything else is the technical state needed to run the feature. Tokens are stored
// only as SHA-256 hashes (upper-case hex), so a copy of the database cannot be used to confirm or unsubscribe anyone.
public sealed class Subscriber
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public SubscriberState State { get; set; } = SubscriberState.Pending;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ConfirmedAtUtc { get; set; }

    /// <summary>Hash of the emailed confirmation token; cleared once the subscriber is confirmed.</summary>
    public string? ConfirmationTokenHash { get; set; }

    /// <summary>When the confirmation link stops working; a pending record is deleted after this moment.</summary>
    public DateTime? ConfirmationTokenExpiresAtUtc { get; set; }

    /// <summary>Hash of the token that identifies this subscriber in an unsubscribe link (used from F10-US2).</summary>
    public string UnsubscribeTokenHash { get; set; } = string.Empty;
}
