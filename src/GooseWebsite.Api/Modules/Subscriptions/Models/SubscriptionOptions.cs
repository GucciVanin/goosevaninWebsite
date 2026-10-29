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

    public AnnouncementOptions Announcements { get; set; } = new();
}

public sealed class SubscriptionRateLimitOptions
{
    /// <summary>Subscribe requests allowed per client address in each window.</summary>
    public int PermitLimit { get; set; } = 5;

    /// <summary>Confirmation requests allowed per client address in each window.</summary>
    public int ConfirmPermitLimit { get; set; } = 20;

    /// <summary>One-click unsubscribe posts allowed per client address in each window. Higher than the others: mail providers post for many subscribers from a few addresses.</summary>
    public int OneClickPermitLimit { get; set; } = 120;

    public int WindowMinutes { get; set; } = 10;
}

/// <summary>How new work is announced to subscribers (F10-US3).</summary>
public sealed class AnnouncementOptions
{
    /// <summary>How often the background job looks for mail that is owed.</summary>
    public int PollSeconds { get; set; } = 20;

    /// <summary>A recipient is tried at most this many times; after that the announcement gives up on them.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>After a failed attempt the recipient waits this many minutes times the attempts so far before the next try.</summary>
    public int RetryDelayMinutes { get; set; } = 10;

    /// <summary>The summary in the email is cut to this many characters.</summary>
    public int SummaryMaxLength { get; set; } = 300;
}
