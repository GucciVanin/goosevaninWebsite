using GooseWebsite.Api.Shared.Announcements;

namespace GooseWebsite.Api.Modules.Subscriptions.Domain;

// One announcement: the first publication of one story or project. It keeps the public facts needed to write the
// email (title, a plain-text summary, the public path) so delivery does not depend on the content still existing.
// It never holds an address or a name. Unique per content item, so a piece of work can be announced only once.
public sealed class Announcement
{
    public int Id { get; set; }

    public AnnouncedContentKind Kind { get; set; }

    public int ContentId { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Plain text, already shortened; the HTML view encodes it.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>The page on this site, for example <c>/stories/my-title</c>.</summary>
    public string PublicPath { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Set when no recipient is left to try (all reached, or given up on).</summary>
    public DateTime? CompletedAtUtc { get; set; }

    public List<AnnouncementDelivery> Deliveries { get; set; } = [];
}

// One recipient of one announcement: the progress record that makes a retry safe. Only ids and counters, never an
// address or a name. Deleted with the announcement and with the subscriber (unsubscribing stops the mail).
public sealed class AnnouncementDelivery
{
    public int Id { get; set; }

    public int AnnouncementId { get; set; }

    public Announcement Announcement { get; set; } = null!;

    public int SubscriberId { get; set; }

    /// <summary>When the provider accepted the mail for this recipient; null while it is still owed.</summary>
    public DateTime? SentAtUtc { get; set; }

    public int Attempts { get; set; }

    public DateTime? LastAttemptAtUtc { get; set; }
}
