using GooseWebsite.Api.Modules.Subscriptions.Domain;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

/// <summary>Sends the subscription emails through the shared mail service.</summary>
public interface ISubscriptionEmailSender
{
    /// <summary>Emails the confirmation link. True when the provider accepted the message.</summary>
    Task<bool> SendConfirmationAsync(int subscriberId, string email, string name, string confirmationUrl);

    /// <summary>Emails one subscriber the announcement of new work, with their own unsubscribe link and headers. True when the provider accepted the message.</summary>
    Task<bool> SendAnnouncementAsync(int subscriberId, string email, string name, Announcement announcement);
}
