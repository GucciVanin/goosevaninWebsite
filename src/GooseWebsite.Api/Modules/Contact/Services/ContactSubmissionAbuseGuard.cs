using System.Security.Cryptography;
using System.Text;

namespace GooseWebsite.Api.Modules.Contact.Services;

// Keeps expired partitions from consuming the bounded contact rate-limit stores. Client addresses
// and recipient addresses are tracked in separate stores so neither can evict the other.
public sealed class ContactSubmissionAbuseGuard : IContactSubmissionAbuseGuard
{
    private const int DefaultMaxTrackedClients = 10000;
    private const int DefaultMaxRequestsPerRecipient = 2;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();
    private readonly PartitionStore _clients = new();
    private readonly PartitionStore _recipients = new();

    public ContactSubmissionAbuseGuard(IConfiguration configuration, TimeProvider? timeProvider = null)
    {
        _configuration = configuration;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool TryAllow(string clientKey, out string? rejectionReason)
    {
        var maxRequests = Math.Max(1, _configuration.GetValue("Contact:RateLimit:MaxRequestsPerWindow", 5));
        var key = string.IsNullOrWhiteSpace(clientKey) ? "unknown" : clientKey;
        return TryAllow(_clients, key, maxRequests, out rejectionReason);
    }

    public bool TryAllowRecipient(string recipientEmail, out string? rejectionReason)
    {
        var maxRequests = Math.Max(1, _configuration.GetValue("Contact:RateLimit:MaxRequestsPerRecipient", DefaultMaxRequestsPerRecipient));

        // Only a hash is kept, so the limiter never holds a sender's address.
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(recipientEmail.Trim().ToLowerInvariant())));
        return TryAllow(_recipients, key, maxRequests, out rejectionReason);
    }

    private bool TryAllow(PartitionStore store, string key, int maxRequests, out string? rejectionReason)
    {
        rejectionReason = null;

        var windowMinutes = Math.Max(1, _configuration.GetValue("Contact:RateLimit:WindowMinutes", 10));
        var maxTrackedClients = Math.Max(1, _configuration.GetValue("Contact:RateLimit:MaxTrackedClients", DefaultMaxTrackedClients));
        var window = TimeSpan.FromMinutes(windowMinutes);
        var now = _timeProvider.GetUtcNow();

        lock (_sync)
        {
            if (now - store.LastCleanupUtc >= CleanupInterval || store.History.Count >= maxTrackedClients)
            {
                RemoveExpiredClients(store, now, window);
                store.LastCleanupUtc = now;
            }

            if (!store.History.TryGetValue(key, out var history))
            {
                if (store.History.Count >= maxTrackedClients)
                {
                    rejectionReason = "rate_limit";
                    return false;
                }

                history = new Queue<DateTimeOffset>();
                store.History.Add(key, history);
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

    private static void RemoveExpiredClients(PartitionStore store, DateTimeOffset now, TimeSpan window)
    {
        var expiredKeys = new List<string>();
        foreach (var pair in store.History)
        {
            RemoveExpiredRequests(pair.Value, now, window);
            if (pair.Value.Count == 0)
            {
                expiredKeys.Add(pair.Key);
            }
        }

        foreach (var key in expiredKeys)
        {
            store.History.Remove(key);
        }
    }

    private static void RemoveExpiredRequests(Queue<DateTimeOffset> history, DateTimeOffset now, TimeSpan window)
    {
        while (history.TryPeek(out var firstSeen) && now - firstSeen >= window)
        {
            history.Dequeue();
        }
    }

    private sealed class PartitionStore
    {
        public Dictionary<string, Queue<DateTimeOffset>> History { get; } = new(StringComparer.Ordinal);
        public DateTimeOffset LastCleanupUtc { get; set; } = DateTimeOffset.MinValue;
    }
}
