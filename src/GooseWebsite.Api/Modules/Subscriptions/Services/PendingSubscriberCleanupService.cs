namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// Hosted job that deletes pending subscribers whose confirmation link has expired (7 days). It runs shortly after
// start and then every hour; a failure is logged by category and the next round tries again.
public sealed class PendingSubscriberCleanupService(IServiceScopeFactory scopes, ILogger<PendingSubscriberCleanupService> logger) : BackgroundService
{
    internal static readonly TimeSpan FirstRunDelay = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstRunDelay, stoppingToken);
            using var timer = new PeriodicTimer(Interval);
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var removed = await scope.ServiceProvider.GetRequiredService<ISubscriptionService>().RemoveExpiredPendingAsync();
            if (removed > 0)
            {
                logger.LogInformation("Expired pending subscriptions removed; count={Count}", removed);
            }
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Pending subscription cleanup failed; exception={ExceptionType}", exception.GetType().Name);
        }
    }
}
