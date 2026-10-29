namespace GooseWebsite.Api.Modules.Subscriptions.Models;

/// <summary>What a subscribe request did. For logs and tests only: the HTTP answer is the same for every value.</summary>
public enum SubscribeOutcome
{
    Honeypot,
    Invalid,
    Created,
    ResentConfirmation,
    ThrottledResend,
    AlreadyConfirmed,
    Raced
}
