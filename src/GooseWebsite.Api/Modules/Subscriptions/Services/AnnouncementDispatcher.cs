using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GooseWebsite.Api.Modules.Subscriptions.Domain;
using GooseWebsite.Api.Modules.Subscriptions.Models;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

/// <summary>What one delivery round did: mails the provider accepted, and attempts that failed (to be retried later).</summary>
public readonly record struct AnnouncementRound(int Sent, int Failed);

/// <summary>Sends the announcement mail that is owed. Safe to call repeatedly: it never mails a recipient twice.</summary>
public interface IAnnouncementDispatcher
{
    Task<AnnouncementRound> DeliverPendingAsync(CancellationToken cancellationToken = default);
}

// One round: find the owed deliveries that are due, send them one at a time, and record each result before moving
// to the next, so a crash or a failing provider never loses track of who was reached. Every delivery runs in its
// own scope (its own database context), so a subscriber who unsubscribes in the middle of a round, or a failure
// on one recipient, cannot disturb the others. A failed recipient waits (RetryDelayMinutes times the attempts so
// far) and is dropped after MaxAttempts. Logs carry counts only: never a name, an address, a title or a token.
// If the process dies between the provider accepting a mail and the record being saved, that one recipient is
// mailed again on the next round: delivery is at least once, and duplicates are limited to that window.
public sealed class AnnouncementDispatcher(
    IServiceScopeFactory scopes,
    TimeProvider clock,
    IOptions<SubscriptionOptions> options,
    ILogger<AnnouncementDispatcher> logger) : IAnnouncementDispatcher
{
    private readonly SemaphoreSlim _oneRoundAtATime = new(1, 1);

    private AnnouncementOptions Settings => options.Value.Announcements;

    public async Task<AnnouncementRound> DeliverPendingAsync(CancellationToken cancellationToken = default)
    {
        await _oneRoundAtATime.WaitAsync(cancellationToken);
        try
        {
            var due = await ListDueAsync(cancellationToken);
            var sent = 0;
            var failed = 0;
            foreach (var deliveryId in due)
            {
                switch (await DeliverOneAsync(deliveryId, cancellationToken))
                {
                    case Outcome.Sent:
                        sent++;
                        break;
                    case Outcome.Failed:
                        failed++;
                        break;
                }
            }

            var closed = await CloseFinishedAsync(cancellationToken);
            if (sent > 0 || failed > 0 || closed > 0)
            {
                logger.LogInformation("Announcement delivery round finished; sent={Sent}; failed={Failed}; announcementsClosed={Closed}", sent, failed, closed);
            }

            return new AnnouncementRound(sent, failed);
        }
        finally
        {
            _oneRoundAtATime.Release();
        }
    }

    private async Task<List<int>> ListDueAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        var owed = await database.Read<AnnouncementDelivery>()
            .Where(delivery => delivery.SentAtUtc == null
                && delivery.Attempts < Settings.MaxAttempts
                && delivery.Announcement.CompletedAtUtc == null)
            .OrderBy(delivery => delivery.AnnouncementId)
            .ThenBy(delivery => delivery.Id)
            .Select(delivery => new { delivery.Id, delivery.Attempts, delivery.LastAttemptAtUtc })
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow().UtcDateTime;
        return owed
            .Where(delivery => delivery.LastAttemptAtUtc is not { } last
                || last + TimeSpan.FromMinutes(Math.Max(0, Settings.RetryDelayMinutes) * delivery.Attempts) <= now)
            .Select(delivery => delivery.Id)
            .ToList();
    }

    private async Task<Outcome> DeliverOneAsync(int deliveryId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
            var delivery = await database.ReadForUpdate<AnnouncementDelivery>()
                .Include(candidate => candidate.Announcement)
                .FirstOrDefaultAsync(candidate => candidate.Id == deliveryId, cancellationToken);
            if (delivery is null || delivery.SentAtUtc is not null)
            {
                return Outcome.Skipped;
            }

            // Read the recipient now: someone who unsubscribed since the round started is gone and gets nothing.
            var subscriber = await database.Read<Subscriber>()
                .Where(candidate => candidate.Id == delivery.SubscriberId && candidate.State == SubscriberState.Confirmed)
                .Select(candidate => new { candidate.Name, candidate.Email })
                .FirstOrDefaultAsync(cancellationToken);
            if (subscriber is null)
            {
                return Outcome.Skipped;
            }

            var emailSender = scope.ServiceProvider.GetRequiredService<ISubscriptionEmailSender>();
            // The token ends the send promptly on shutdown: the recipient is then neither sent nor counted as failed,
            // and the next round starts from the same state.
            var accepted = await emailSender.SendAnnouncementAsync(delivery.SubscriberId, subscriber.Email, subscriber.Name, delivery.Announcement, cancellationToken);
            if (!accepted)
            {
                // A mail service that reports a cancelled send as a failure must not burn a retry on shutdown.
                cancellationToken.ThrowIfCancellationRequested();
            }

            var now = clock.GetUtcNow().UtcDateTime;
            if (accepted)
            {
                delivery.SentAtUtc = now;
            }
            else
            {
                delivery.Attempts++;
                delivery.LastAttemptAtUtc = now;
            }

            await database.SaveChangesAsync();
            return accepted ? Outcome.Sent : Outcome.Failed;
        }
        catch (DbUpdateConcurrencyException)
        {
            // The subscriber unsubscribed while the mail was being sent; the record is already gone.
            return Outcome.Skipped;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError("Announcement delivery failed for one recipient; exception={ExceptionType}", exception.GetType().Name);
            return Outcome.Failed;
        }
    }

    // An announcement is finished when no recipient is still owed mail that can be tried: all reached or given up on.
    private async Task<int> CloseFinishedAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        var finished = await database.ReadForUpdate<Announcement>()
            .Where(announcement => announcement.CompletedAtUtc == null
                && !announcement.Deliveries.Any(delivery => delivery.SentAtUtc == null && delivery.Attempts < Settings.MaxAttempts))
            .ToListAsync(cancellationToken);
        if (finished.Count == 0)
        {
            return 0;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var announcement in finished)
        {
            announcement.CompletedAtUtc = now;
        }

        await database.SaveChangesAsync();
        return finished.Count;
    }

    private enum Outcome
    {
        Skipped,
        Sent,
        Failed
    }
}
