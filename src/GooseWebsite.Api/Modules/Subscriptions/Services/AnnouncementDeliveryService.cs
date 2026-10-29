using Microsoft.Extensions.Options;
using GooseWebsite.Api.Modules.Subscriptions.Models;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// Hosted job that sends the announcement mail owed to subscribers, so Gustavo's publish request never waits for a
// mail provider and a provider failure never reaches him. It wakes every PollSeconds (a failed recipient is
// retried by later rounds, see AnnouncementDispatcher) and survives any failure of a round: the next one tries again.
// Because the owed mail is stored in the database, a restart loses nothing.
public sealed class AnnouncementDeliveryService(
    IAnnouncementDispatcher dispatcher,
    IOptions<SubscriptionOptions> options,
    ILogger<AnnouncementDeliveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.Value.Announcements.PollSeconds)), stoppingToken);
                try
                {
                    await dispatcher.DeliverPendingAsync(stoppingToken);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError("Announcement delivery round failed; exception={ExceptionType}", exception.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }
}
