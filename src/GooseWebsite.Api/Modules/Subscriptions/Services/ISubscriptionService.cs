using GooseWebsite.Api.Modules.Subscriptions.Contracts;
using GooseWebsite.Api.Modules.Subscriptions.Models;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

/// <summary>The module's rules for subscribing, confirming and cleaning up. The controller maps these to HTTP only.</summary>
public interface ISubscriptionService
{
    /// <summary>Handles one subscribe request. Never throws for bad input and never reveals whether the address is known.</summary>
    Task<SubscribeOutcome> SubscribeAsync(SubscribeRequest request);

    /// <summary>True when the token was valid, unused and unexpired, and the subscriber is now confirmed.</summary>
    Task<bool> ConfirmAsync(string? token);

    /// <summary>Deletes pending subscribers whose confirmation link has expired. Returns how many.</summary>
    Task<int> RemoveExpiredPendingAsync();
}
