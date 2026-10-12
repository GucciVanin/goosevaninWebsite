namespace GooseWebsite.Api.Modules.Contact.Models;

/// <summary>What happened to a Contact submission, so the web layer can answer without knowing the rules.</summary>
public enum ContactSubmitStatus
{
    /// <summary>A Verification link was emailed to the sender and a Pending message is held.</summary>
    Accepted,

    /// <summary>The hidden field was filled in, so the submission was treated as bot traffic.</summary>
    Honeypot,

    MissingFields,
    InvalidReason,
    InvalidEmail,

    /// <summary>This device has submitted too often.</summary>
    ClientRateLimited,

    /// <summary>This address has been sent too many Verification links.</summary>
    RecipientRateLimited,

    /// <summary>The Verification link could not be sent; no Pending message is kept.</summary>
    EmailNotSent
}

public sealed record ContactSubmitOutcome(ContactSubmitStatus Status);
