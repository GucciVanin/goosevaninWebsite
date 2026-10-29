namespace GooseWebsite.Api.Modules.Subscriptions.Services;

/// <summary>Sends the subscription emails through the shared mail service.</summary>
public interface ISubscriptionEmailSender
{
    /// <summary>Emails the confirmation link. True when the provider accepted the message.</summary>
    Task<bool> SendConfirmationAsync(string email, string name, string confirmationUrl);
}
