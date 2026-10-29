namespace GooseWebsite.Api.Modules.Subscriptions.Models;

/// <summary>Settings under <c>Subscriptions</c>. The defaults are the decided behavior; tests change them.</summary>
public sealed class SubscriptionOptions
{
    public const string SectionName = "Subscriptions";

    /// <summary>How long the emailed confirmation link works, and how long a pending record is kept.</summary>
    public int ConfirmationLifetimeDays { get; set; } = 7;

    /// <summary>A pending address is not mailed again sooner than this, so the form cannot flood an inbox.</summary>
    public int ResendCooldownMinutes { get; set; } = 2;

    public SubscriptionRateLimitOptions RateLimit { get; set; } = new();
}

public sealed class SubscriptionRateLimitOptions
{
    /// <summary>Subscribe requests allowed per client address in each window.</summary>
    public int PermitLimit { get; set; } = 5;

    /// <summary>Confirmation requests allowed per client address in each window.</summary>
    public int ConfirmPermitLimit { get; set; } = 20;

    public int WindowMinutes { get; set; } = 10;
}
