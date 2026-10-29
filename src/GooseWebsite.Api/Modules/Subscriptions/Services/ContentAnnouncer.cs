using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GooseWebsite.Api.Modules.Subscriptions.Domain;
using GooseWebsite.Api.Modules.Subscriptions.Models;
using GooseWebsite.Api.Shared.Announcements;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// Listens for the first publication of a story or project (Blog and Projects call IContentAnnouncer after it) and
// queues the announcement: one record for the work and one delivery record per confirmed subscriber at this moment.
// It sends nothing and talks to no mail provider; the hosted delivery job does that later. The rows are saved in
// the publish request's own unit of work (UnitOfWorkFilter): this save and the caller's earlier save share one
// transaction that commits when the action ends, so the publish, its marker and the queue rows are committed together
// or not at all. If this method throws, everything rolls back and the publish request fails and can be repeated.
// A person who confirms later is not told about earlier work; one who unsubscribes first is dropped (the database
// removes their delivery record with them).
public sealed class ContentAnnouncer(
    DatabaseService database,
    TimeProvider clock,
    IOptions<SubscriptionOptions> options,
    ILogger<ContentAnnouncer> logger) : IContentAnnouncer
{
    public async Task AnnounceAsync(ContentAnnouncement announcement, CancellationToken cancellationToken = default)
    {
        // A piece of work is announced at most once, even if the caller's own marker were ever lost.
        var alreadyAnnounced = await database.Read<Announcement>()
            .AnyAsync(candidate => candidate.Kind == announcement.Kind && candidate.ContentId == announcement.ContentId, cancellationToken);
        if (alreadyAnnounced)
        {
            return;
        }

        var recipients = await database.Read<Subscriber>()
            .Where(subscriber => subscriber.State == SubscriberState.Confirmed)
            .Select(subscriber => subscriber.Id)
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow().UtcDateTime;
        var record = new Announcement
        {
            Kind = announcement.Kind,
            ContentId = announcement.ContentId,
            Title = PublicText.Title(announcement.Title, AnnouncementAccessRule.TitleMaxLength),
            Summary = PublicText.Summary(announcement.Summary, Math.Min(options.Value.Announcements.SummaryMaxLength, AnnouncementAccessRule.SummaryMaxLength)),
            PublicPath = announcement.PublicPath,
            CreatedAtUtc = now,
            CompletedAtUtc = recipients.Count == 0 ? now : null,
            Deliveries = recipients.Select(id => new AnnouncementDelivery { SubscriberId = id }).ToList()
        };
        database.Add(record);
        await database.SaveChangesAsync();

        logger.LogInformation("Announcement queued; kind={Kind}; recipients={Count}", announcement.Kind, recipients.Count);
    }
}
