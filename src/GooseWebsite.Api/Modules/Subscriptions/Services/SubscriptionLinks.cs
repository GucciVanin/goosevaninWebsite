using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using GooseWebsite.Api.Shared.Hosting;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// The unsubscribe token is never stored. It is the subscriber id sealed with the ASP.NET Data Protection key ring
// (authenticated encryption; the ring is persisted in the data volume, see AccountsModule), so only this server can
// make or read one, a tampered or foreign token fails the integrity check (compared in constant time inside
// Data Protection), and it stays valid until the subscriber is deleted: key rotation keeps old keys for reading.
// Ids are not reused, so a link for a deleted subscriber can never reach a later one.
public sealed class SubscriptionLinks(IDataProtectionProvider protection, PublicLinkBuilder links) : ISubscriptionLinks
{
    private const string Purpose = "GooseWebsite.Subscriptions.Unsubscribe.v1";
    private const string OneClickPath = "/api/subscriptions/unsubscribe/one-click";

    private readonly IDataProtector _protector = protection.CreateProtector(Purpose);

    public string UnsubscribePageUrl(int subscriberId) => links.FragmentLink("/unsubscribe", ("token", Issue(subscriberId)));

    public string OneClickUnsubscribeUrl(int subscriberId) =>
        links.SiteLink($"{OneClickPath}?token={Uri.EscapeDataString(Issue(subscriberId))}");

    public IReadOnlyDictionary<string, string> UnsubscribeHeaders(int subscriberId) => new Dictionary<string, string>
    {
        ["List-Unsubscribe"] = $"<{OneClickUnsubscribeUrl(subscriberId)}>",
        ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click"
    };

    public bool TryReadSubscriberId(string? token, out int subscriberId)
    {
        subscriberId = 0;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            return int.TryParse(_protector.Unprotect(token.Trim()), NumberStyles.None, CultureInfo.InvariantCulture, out subscriberId) && subscriberId > 0;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private string Issue(int subscriberId) => _protector.Protect(subscriberId.ToString(CultureInfo.InvariantCulture));
}
