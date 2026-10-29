using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GooseWebsite.Api.Modules.Subscriptions.Contracts;
using GooseWebsite.Api.Modules.Subscriptions.Domain;
using GooseWebsite.Api.Modules.Subscriptions.Models;
using GooseWebsite.Api.Shared.Hosting;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// Owns the double opt-in rules. Everything it learns about a visitor stays in the Subscribers table: logs carry
// the outcome category only, never a name, an address or a token.
public sealed partial class SubscriptionService(
    DatabaseService database,
    ISubscriptionEmailSender emailSender,
    PublicLinkBuilder publicLinks,
    ISubscriptionLinks links,
    TimeProvider clock,
    IOptions<SubscriptionOptions> options,
    ILogger<SubscriptionService> logger) : ISubscriptionService
{
    private const int MaxTokenLength = 200;
    private const int MaxUnsubscribeTokenLength = 1000;

    private SubscriptionOptions Settings => options.Value;

    public async Task<SubscribeOutcome> SubscribeAsync(SubscribeRequest request)
    {
        var outcome = await HandleSubscribeAsync(request);
        logger.LogInformation("Subscription request handled; outcome={Outcome}", outcome);
        return outcome;
    }

    private async Task<SubscribeOutcome> HandleSubscribeAsync(SubscribeRequest request)
    {
        // A filled honeypot means a script, not a person: store nothing and send nothing.
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            return SubscribeOutcome.Honeypot;
        }

        var name = CleanName(request.Name);
        var email = request.Email?.Trim() ?? string.Empty;
        if (!IsValidName(name) || !IsValidEmail(email))
        {
            return SubscribeOutcome.Invalid;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var lifetime = TimeSpan.FromDays(Math.Max(1, Settings.ConfirmationLifetimeDays));
        var existing = await database.ReadForUpdate<Subscriber>().FirstOrDefaultAsync(subscriber => subscriber.Email == email);
        var token = SubscriptionTokens.New();

        if (existing is null)
        {
            existing = new Subscriber
            {
                Name = name,
                Email = email,
                State = SubscriberState.Pending,
                CreatedAtUtc = now,
                ConfirmationTokenHash = SubscriptionTokens.Hash(token),
                ConfirmationTokenExpiresAtUtc = now + lifetime
            };
            database.Add(existing);
            if (!await TrySaveAsync())
            {
                return SubscribeOutcome.Raced;
            }

            await SendConfirmationAsync(existing, token);
            return SubscribeOutcome.Created;
        }

        if (existing.State == SubscriberState.Confirmed)
        {
            return SubscribeOutcome.AlreadyConfirmed;
        }

        // The mail time is the expiry minus the lifetime, so no extra column is needed to throttle by address.
        var lastMailed = (existing.ConfirmationTokenExpiresAtUtc ?? now) - lifetime;
        if (now - lastMailed < TimeSpan.FromMinutes(Math.Max(0, Settings.ResendCooldownMinutes)))
        {
            return SubscribeOutcome.ThrottledResend;
        }

        existing.Name = name;
        existing.ConfirmationTokenHash = SubscriptionTokens.Hash(token);
        existing.ConfirmationTokenExpiresAtUtc = now + lifetime;
        await database.SaveChangesAsync();
        await SendConfirmationAsync(existing, token);
        return SubscribeOutcome.ResentConfirmation;
    }

    public async Task<bool> ConfirmAsync(string? token)
    {
        token = token?.Trim();
        if (string.IsNullOrEmpty(token) || token.Length > MaxTokenLength)
        {
            return false;
        }

        var hash = SubscriptionTokens.Hash(token);
        var now = clock.GetUtcNow().UtcDateTime;
        var subscriber = await database.ReadForUpdate<Subscriber>()
            .FirstOrDefaultAsync(candidate => candidate.ConfirmationTokenHash == hash && candidate.State == SubscriberState.Pending);
        if (subscriber is null || subscriber.ConfirmationTokenExpiresAtUtc is not { } expires || expires <= now)
        {
            return false;
        }

        subscriber.State = SubscriberState.Confirmed;
        subscriber.ConfirmedAtUtc = now;
        subscriber.ConfirmationTokenHash = null;
        subscriber.ConfirmationTokenExpiresAtUtc = null;
        await database.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnsubscribeAsync(string? token)
    {
        token = token?.Trim();
        if (string.IsNullOrEmpty(token) || token.Length > MaxUnsubscribeTokenLength || !links.TryReadSubscriberId(token, out var id))
        {
            return false;
        }

        // Ids are never reused, so a link for a deleted subscriber finds nothing and cannot touch a later subscription.
        var subscriber = await database.ReadForUpdate<Subscriber>().FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (subscriber is null)
        {
            return false;
        }

        database.Remove(subscriber);
        await database.SaveChangesAsync();
        logger.LogInformation("Subscriber removed by an unsubscribe request");
        return true;
    }

    public async Task<int> RemoveExpiredPendingAsync()
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expired = await database.ReadForUpdate<Subscriber>()
            .Where(subscriber => subscriber.State == SubscriberState.Pending
                && subscriber.ConfirmationTokenExpiresAtUtc != null
                && subscriber.ConfirmationTokenExpiresAtUtc <= now)
            .ToListAsync();
        foreach (var subscriber in expired)
        {
            database.Remove(subscriber);
        }

        if (expired.Count > 0)
        {
            await database.SaveChangesAsync();
        }

        return expired.Count;
    }

    // A second request for the same new address can win the race to the unique index; the loser looks the same to the caller.
    private async Task<bool> TrySaveAsync()
    {
        try
        {
            await database.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    private async Task SendConfirmationAsync(Subscriber subscriber, string token)
    {
        var link = publicLinks.FragmentLink("/subscribe/confirm", ("token", token));
        if (!await emailSender.SendConfirmationAsync(subscriber.Id, subscriber.Email, subscriber.Name, link))
        {
            // The record stays pending; the visitor can submit again. The answer is the same either way.
            logger.LogWarning("Subscription confirmation email was not sent");
        }
    }

    // One line, no control characters (they would otherwise reach a mail header through the display name).
    // Format characters (Cf: bidirectional overrides, zero-width marks, tag characters) and the line and paragraph
    // separators (Zl, Zp) are removed too: they can hide or reorder text in a mail client or in the editor.
    private static string CleanName(string? name) =>
        string.Concat((name ?? string.Empty).EnumerateRunes().Where(rune => !IsUnwanted(rune)).Select(rune => rune.ToString())).Trim();

    private static bool IsUnwanted(Rune rune) =>
        Rune.IsControl(rune)
        || Rune.GetUnicodeCategory(rune) is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator or UnicodeCategory.Format;

    private static bool IsValidName(string name) => name.Length is > 0 and <= SubscriberAccessRule.NameMaxLength;

    private static bool IsValidEmail(string email)
    {
        if (email.Length is 0 or > SubscriberAccessRule.EmailMaxLength
            || email.Any(character => char.IsWhiteSpace(character) || char.IsControl(character) || character is ',' or ';' or '<' or '>'))
        {
            return false;
        }

        return EmailShape.IsMatch(email);
    }

    // local@domain.tld in plain ASCII: no display names, no quoted or comment forms, no consecutive or edge dots.
    [GeneratedRegex(@"^[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@([A-Za-z0-9]([A-Za-z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z]{2,}$")]
    private static partial Regex EmailShape { get; }
}
