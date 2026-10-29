using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;
using GooseWebsite.Api.Modules.Subscriptions.Domain;
using GooseWebsite.Api.Shared.Modules;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// Every unsubscribe link already mailed is sealed with the Data Protection key ring (see SubscriptionLinks), so the
// ring is as important as the database: if it is lost, those links stop working and subscribers cannot opt out. At
// startup, before the ring would create a fresh key, this notices "no usable key but subscribers exist" and logs one
// warning (category only: no count, no address, no name). Restore the key directory from the backup that goes with
// the database (Docs/architecture.md, section 7.3); never revoke the keys.
public sealed class DataProtectionKeyRingGuard(IKeyManager keys, DatabaseService database, ILogger<DataProtectionKeyRingGuard> logger) : IModuleInitializer
{
    public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default) => CheckAsync(cancellationToken);

    public async Task CheckAsync(CancellationToken cancellationToken = default)
    {
        if (keys.GetAllKeys().Any(key => !key.IsRevoked))
        {
            return;
        }

        if (await database.Read<Subscriber>().AnyAsync(cancellationToken))
        {
            logger.LogWarning(
                "The Data Protection key ring is new or empty while subscribers exist: unsubscribe links already sent in email will not work until the original key directory is restored");
        }
    }
}
