namespace VaninWebsite.Api.Modules.Contact.Services;

// Keeps expired client partitions from consuming the bounded contact rate-limit store.
public sealed class ContactSubmissionAbuseGuard : IContactSubmissionAbuseGuard
{
    private const int DefaultMaxTrackedClients = 10000;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();
    private readonly Dictionary<string, Queue<DateTimeOffset>> _requestHistory = new(StringComparer.Ordinal);
    private DateTimeOffset _lastCleanupUtc = DateTimeOffset.MinValue;

    public ContactSubmissionAbuseGuard(IConfiguration configuration, TimeProvider? timeProvider = null)
    {
        _configuration = configuration;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool TryAllow(string clientKey, out string? rejectionReason)
    {
        rejectionReason = null;

        var maxRequests = Math.Max(1, _configuration.GetValue("Contact:RateLimit:MaxRequestsPerWindow", 5));
        var windowMinutes = Math.Max(1, _configuration.GetValue("Contact:RateLimit:WindowMinutes", 10));
        var maxTrackedClients = Math.Max(1, _configuration.GetValue("Contact:RateLimit:MaxTrackedClients", DefaultMaxTrackedClients));
        var window = TimeSpan.FromMinutes(windowMinutes);
        var key = string.IsNullOrWhiteSpace(clientKey) ? "unknown" : clientKey;
        var now = _timeProvider.GetUtcNow();

        lock (_sync)
        {
            if (now - _lastCleanupUtc >= CleanupInterval || _requestHistory.Count >= maxTrackedClients)
            {
                RemoveExpiredClients(now, window);
                _lastCleanupUtc = now;
            }

            if (!_requestHistory.TryGetValue(key, out var history))
            {
                if (_requestHistory.Count >= maxTrackedClients)
                {
                    rejectionReason = "rate_limit";
                    return false;
                }

                history = new Queue<DateTimeOffset>();
                _requestHistory.Add(key, history);
            }

            RemoveExpiredRequests(history, now, window);
            if (history.Count >= maxRequests)
            {
                rejectionReason = "rate_limit";
                return false;
            }

            history.Enqueue(now);
            return true;
        }
    }

    private void RemoveExpiredClients(DateTimeOffset now, TimeSpan window)
    {
        var expiredClientKeys = new List<string>();
        foreach (var pair in _requestHistory)
        {
            RemoveExpiredRequests(pair.Value, now, window);
            if (pair.Value.Count == 0)
            {
                expiredClientKeys.Add(pair.Key);
            }
        }

        foreach (var clientKey in expiredClientKeys)
        {
            _requestHistory.Remove(clientKey);
        }
    }

    private static void RemoveExpiredRequests(Queue<DateTimeOffset> history, DateTimeOffset now, TimeSpan window)
    {
        while (history.TryPeek(out var firstSeen) && now - firstSeen >= window)
        {
            history.Dequeue();
        }
    }
}
