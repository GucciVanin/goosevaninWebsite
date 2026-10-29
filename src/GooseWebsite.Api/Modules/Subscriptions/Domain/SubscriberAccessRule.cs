using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Subscriptions.Domain;

// Visitors subscribe without signing in, so an anonymous request writes these rows (Anyone). The rows are never
// returned by any endpoint; the module's services are the only readers. The name and the email are text the
// visitor typed: trimmed, cleaned, and limited (name 100, email 254).
public sealed class SubscriberAccessRule : EntityAccessRule<Subscriber>
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;

    public SubscriberAccessRule() : base(WriteAccess.Anyone)
    {
        IsUserText(subscriber => subscriber.Name, NameMaxLength);
        IsUserText(subscriber => subscriber.Email, EmailMaxLength);
    }
}
