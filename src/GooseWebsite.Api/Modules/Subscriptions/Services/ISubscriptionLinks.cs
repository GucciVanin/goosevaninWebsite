namespace GooseWebsite.Api.Modules.Subscriptions.Services;

/// <summary>
/// Builds and reads the unsubscribe links of subscribers. Every mail the feature sends (the confirmation now, the
/// announcements in F10-US3) takes its unsubscribe link and its <c>List-Unsubscribe</c> headers from here.
/// </summary>
public interface ISubscriptionLinks
{
    /// <summary>The client page link for the body of a mail: <c>{PublicBaseUrl}/unsubscribe#token=...</c> (the token stays in the fragment).</summary>
    string UnsubscribePageUrl(int subscriberId);

    /// <summary>The one-click endpoint for mail clients (RFC 8058): an HTTPS address that accepts a POST.</summary>
    string OneClickUnsubscribeUrl(int subscriberId);

    /// <summary>The <c>List-Unsubscribe</c> and <c>List-Unsubscribe-Post</c> headers to put on a mail to this subscriber.</summary>
    IReadOnlyDictionary<string, string> UnsubscribeHeaders(int subscriberId);

    /// <summary>True, with the subscriber id, when the token was issued by this server's key ring and is intact.</summary>
    bool TryReadSubscriberId(string? token, out int subscriberId);
}
