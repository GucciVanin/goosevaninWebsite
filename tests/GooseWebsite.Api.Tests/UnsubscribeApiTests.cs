using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GooseWebsite.Api.Modules.Subscriptions.Services;
using GooseWebsite.Api.Shared.Email;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F10-US2: a subscriber leaves from any email without signing in and the record (name and email) is deleted.
// The unsubscribe link carries a token derived from the subscriber id with the server key ring: nothing is stored
// for it, it is valid until the subscriber is deleted, and a mail scanner that fetches the link changes nothing.
public sealed partial class SubscriptionsApiTests
{
    private static string UnsubscribeTokenFromLastMail(Harness h)
    {
        var mail = h.Factory.Mail.Rendered.Last();
        var match = Regex.Match(mail.PlainText, @"/unsubscribe#token=([A-Za-z0-9_\-%.]+)");
        Assert.True(match.Success, "The mail carries the link to /unsubscribe with the token in the fragment.");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    private static async Task<(HttpStatusCode Status, string Body)> UnsubscribeAsync(Harness h, string? token)
    {
        var response = await h.PostAsync("/api/subscriptions/unsubscribe", new { token });
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task<HttpResponseMessage> OneClickAsync(Harness h, string token, string formBody = "List-Unsubscribe=One-Click")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/subscriptions/unsubscribe/one-click?token={Uri.EscapeDataString(token)}")
        {
            Content = new StringContent(formBody, Encoding.UTF8, "application/x-www-form-urlencoded")
        };
        return await h.Client.SendAsync(request);
    }

    [Fact]
    public async Task Unsubscribe_DeletesTheConfirmedSubscriberAndOnlyThatOne()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var confirm = h.TokenFromLastMail();
        var unsubscribe = UnsubscribeTokenFromLastMail(h);
        await h.ConfirmAsync(confirm);
        await h.SubscribeAsync("Grace Other", "grace.other@example.test");
        Assert.Equal(2, (await h.StoredAsync()).Count);

        var (status, body) = await UnsubscribeAsync(h, unsubscribe);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains("unsubscribed", body, StringComparison.OrdinalIgnoreCase);
        var remaining = Assert.Single(await h.StoredAsync());
        Assert.Equal("grace.other@example.test", remaining.Email);
    }

    [Fact]
    public async Task Unsubscribe_AlsoRemovesAPendingSubscriber()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);

        var (status, _) = await UnsubscribeAsync(h, UnsubscribeTokenFromLastMail(h));

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Empty(await h.StoredAsync());
    }

    [Fact]
    public async Task Unsubscribe_RefusesUsedTamperedAndUnknownLinksWithOneNeutralAnswerAndChangesNothing()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var token = UnsubscribeTokenFromLastMail(h);
        await h.SubscribeAsync("Grace Other", "grace.other@example.test");
        var graceToken = UnsubscribeTokenFromLastMail(h);
        await UnsubscribeAsync(h, token);
        Assert.Single(await h.StoredAsync());

        var used = await UnsubscribeAsync(h, token);
        var tampered = await UnsubscribeAsync(h, graceToken[..^3] + (graceToken.EndsWith("AAA") ? "BBB" : "AAA"));
        var unknown = await UnsubscribeAsync(h, "an-unknown-token-value");
        var empty = await UnsubscribeAsync(h, "   ");
        var missing = await UnsubscribeAsync(h, null);
        var tooLong = await UnsubscribeAsync(h, new string('a', 5000));

        foreach (var answer in new[] { used, tampered, unknown, empty, missing, tooLong })
        {
            Assert.Equal(HttpStatusCode.BadRequest, answer.Status);
            Assert.Equal(used.Body, answer.Body);
        }

        Assert.Contains("no longer valid", used.Body);
        Assert.DoesNotContain("exist", used.Body, StringComparison.OrdinalIgnoreCase);
        var remaining = Assert.Single(await h.StoredAsync());
        Assert.Equal("grace.other@example.test", remaining.Email);
    }

    [Fact]
    public async Task AnOldUnsubscribeLinkNeverRemovesALaterSubscriptionOfTheSameAddress()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var oldToken = UnsubscribeTokenFromLastMail(h);
        await UnsubscribeAsync(h, oldToken);
        await h.SubscribeAsync(SubscriberName, Email);
        var newToken = UnsubscribeTokenFromLastMail(h);

        var (status, _) = await UnsubscribeAsync(h, oldToken);

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Single(await h.StoredAsync());
        Assert.NotEqual(oldToken, newToken);
    }

    [Fact]
    public async Task TheUnsubscribeLinkIsNotStored_AndEveryLinkEverMailedStaysValid()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var first = UnsubscribeTokenFromLastMail(h);
        h.Clock.Advance(TimeSpan.FromMinutes(10));
        await h.SubscribeAsync(SubscriberName, Email);
        var second = UnsubscribeTokenFromLastMail(h);

        using (var scope = h.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var connection = (SqliteConnection)context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Subscribers";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = Convert.ToString(reader.GetValue(i)) ?? string.Empty;
                    Assert.DoesNotContain(first, value, StringComparison.Ordinal);
                    Assert.DoesNotContain(second, value, StringComparison.Ordinal);
                }
            }
        }

        // The older mail still works: nothing replaced it, because nothing was stored.
        Assert.Equal(HttpStatusCode.OK, (await UnsubscribeAsync(h, first)).Status);
        Assert.Empty(await h.StoredAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await UnsubscribeAsync(h, second)).Status);
    }

    [Fact]
    public async Task TheUnsubscribeLinkStaysValidUntilTheSubscriberIsDeleted()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var token = UnsubscribeTokenFromLastMail(h);
        await h.ConfirmAsync(h.TokenFromLastMail());
        h.Clock.Advance(TimeSpan.FromDays(400));

        var (status, _) = await UnsubscribeAsync(h, token);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Empty(await h.StoredAsync());
    }

    [Fact]
    public async Task TheSharedLinkServiceReadsItsOwnTokens_AndAnotherKeyRingCannotForgeOrReadThem()
    {
        string token;
        using (var first = new Harness())
        {
            await first.SubscribeAsync(SubscriberName, Email);
            token = UnsubscribeTokenFromLastMail(first);
            using var scope = first.Factory.Services.CreateScope();
            var links = scope.ServiceProvider.GetRequiredService<ISubscriptionLinks>();
            var stored = Assert.Single(await first.StoredAsync());
            var rebuilt = Uri.UnescapeDataString(links.UnsubscribePageUrl(stored.Id).Split("#token=")[1]);
            Assert.True(links.TryReadSubscriberId(rebuilt, out var rebuiltId));
            Assert.Equal(stored.Id, rebuiltId);
            Assert.True(links.TryReadSubscriberId(token, out var id));
            Assert.Equal(stored.Id, id);
        }

        using var other = new Harness();
        using var otherScope = other.Factory.Services.CreateScope();
        Assert.False(otherScope.ServiceProvider.GetRequiredService<ISubscriptionLinks>().TryReadSubscriberId(token, out _));
    }

    [Fact]
    public async Task UnsubscribingNeedsAPostWithTheCsrfToken_AndFetchingTheLinksChangesNothing()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var token = UnsubscribeTokenFromLastMail(h);

        var withoutCsrf = await h.PostAsync("/api/subscriptions/unsubscribe", new { token }, withCsrf: false);
        var getApi = await h.Client.GetAsync("/api/subscriptions/unsubscribe?token=" + Uri.EscapeDataString(token));
        var getOneClick = await h.Client.GetAsync("/api/subscriptions/unsubscribe/one-click?token=" + Uri.EscapeDataString(token));
        var getPage = await h.Client.GetAsync("/unsubscribe?token=" + Uri.EscapeDataString(token));

        Assert.Equal(HttpStatusCode.BadRequest, withoutCsrf.StatusCode);
        Assert.DoesNotContain("unsubscribed", await getApi.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unsubscribed", await getOneClick.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.True(getPage.IsSuccessStatusCode);
        Assert.Single(await h.StoredAsync());
    }

    [Fact]
    public async Task TheOneClickEndpointForMailClientsNeedsThePostBodyAndNeedsNoCsrfToken()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var token = UnsubscribeTokenFromLastMail(h);

        var wrongBody = await OneClickAsync(h, token, "something=else");
        var noBody = await OneClickAsync(h, token, "");
        var badToken = await OneClickAsync(h, "an-unknown-token-value");
        Assert.Equal(HttpStatusCode.BadRequest, wrongBody.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noBody.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badToken.StatusCode);
        Assert.Single(await h.StoredAsync());

        var ok = await OneClickAsync(h, token);

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Empty(await h.StoredAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await OneClickAsync(h, token)).StatusCode);
    }

    [Fact]
    public async Task EverySubscriptionMailCarriesTheUnsubscribeLinkAndTheListUnsubscribeHeaders()
    {
        using var h = new Harness();

        await h.SubscribeAsync(SubscriberName, Email);

        var mail = Assert.Single(h.Factory.Mail.Rendered);
        var token = UnsubscribeTokenFromLastMail(h);
        Assert.Contains("https://localhost/unsubscribe#token=", mail.PlainText);
        Assert.Contains("https://localhost/unsubscribe#token=", mail.Html);
        Assert.NotNull(mail.Headers);
        var header = mail.Headers!["List-Unsubscribe"];
        var match = Regex.Match(header, @"^<https://localhost/api/subscriptions/unsubscribe/one-click\?token=([A-Za-z0-9_\-%.]+)>$");
        Assert.True(match.Success, "The header is one link in angle brackets.");
        using var scope = h.Factory.Services.CreateScope();
        var links = scope.ServiceProvider.GetRequiredService<ISubscriptionLinks>();
        var stored = Assert.Single(await h.StoredAsync());
        Assert.True(links.TryReadSubscriberId(Uri.UnescapeDataString(match.Groups[1].Value), out var headerId));
        Assert.Equal(stored.Id, headerId);
        Assert.True(links.TryReadSubscriberId(token, out var pageId));
        Assert.Equal(stored.Id, pageId);
        Assert.Equal("List-Unsubscribe=One-Click", mail.Headers["List-Unsubscribe-Post"]);
        // The confirmation link stays the first link in the text.
        Assert.True(mail.PlainText.IndexOf("/subscribe/confirm#", StringComparison.Ordinal) < mail.PlainText.IndexOf("/unsubscribe#", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheSharedLinkServiceGivesLaterMailsTheSameLinkAndHeaders()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var stored = Assert.Single(await h.StoredAsync());
        var mail = h.Factory.Mail.Rendered.Last();

        using var scope = h.Factory.Services.CreateScope();
        var links = scope.ServiceProvider.GetRequiredService<ISubscriptionLinks>();

        var headers = links.UnsubscribeHeaders(stored.Id);
        Assert.StartsWith("https://localhost/unsubscribe#token=", links.UnsubscribePageUrl(stored.Id));
        Assert.StartsWith("<https://localhost/api/subscriptions/unsubscribe/one-click?token=", headers["List-Unsubscribe"]);
        Assert.EndsWith(">", headers["List-Unsubscribe"]);
        Assert.Equal(mail.Headers!["List-Unsubscribe-Post"], headers["List-Unsubscribe-Post"]);
        Assert.Equal(mail.Headers["List-Unsubscribe"][..40], headers["List-Unsubscribe"][..40]);
    }

    [Fact]
    public void TheSmtpTransportWritesTheCustomHeadersIntoTheMessage()
    {
        var mail = new RenderedMail(
            new MailAddressee("to@example.test", "To"),
            "Subject",
            "<p>html</p>",
            "text",
            new Dictionary<string, string> { ["List-Unsubscribe"] = "<https://localhost/x>", ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click" });

        using var message = SmtpMailTransport.CreateMessage(mail, "from@example.test", "From");

        Assert.Equal("<https://localhost/x>", message.Headers["List-Unsubscribe"]);
        Assert.Equal("List-Unsubscribe=One-Click", message.Headers["List-Unsubscribe-Post"]);
    }

    [Fact]
    public void AMailWithoutCustomHeadersIsStillValid()
    {
        var mail = new RenderedMail(new MailAddressee("to@example.test", "To"), "S", "<p/>", "t");

        using var message = SmtpMailTransport.CreateMessage(mail, "from@example.test", "From");

        Assert.Null(mail.Headers);
        Assert.Null(message.Headers["List-Unsubscribe"]);
    }

    [Fact]
    public async Task TheUnsubscribeEndpointIsRateLimitedPerClient()
    {
        using var h = new Harness(confirmPermitLimit: 2);

        var first = await UnsubscribeAsync(h, "a");
        var second = await UnsubscribeAsync(h, "b");
        var third = await UnsubscribeAsync(h, "c");

        Assert.Equal(HttpStatusCode.BadRequest, first.Status);
        Assert.Equal(HttpStatusCode.BadRequest, second.Status);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.Status);
    }

    [Fact]
    public async Task UnsubscribeTokensAndAddressesAreNeverLogged()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var token = UnsubscribeTokenFromLastMail(h);
        await UnsubscribeAsync(h, "an-unknown-unsubscribe-value");
        await UnsubscribeAsync(h, token);
        await h.SubscribeAsync(SubscriberName, Email);
        var again = UnsubscribeTokenFromLastMail(h);
        await OneClickAsync(h, again);

        var log = string.Join('\n', h.Factory.Logs.Messages);

        foreach (var secret in new[] { Email, "ada.subscriber", SubscriberName, token, again, "an-unknown-unsubscribe-value" })
        {
            Assert.DoesNotContain(secret, log, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task TheNameLosesLineSeparatorsParagraphSeparatorsAndFormatCharacters()
    {
        using var h = new Harness();
        // U+2028 (Zl), U+2029 (Zp), U+202E right-to-left override, U+200B zero width space, U+FEFF (Cf) and a tag letter outside the BMP (Cf).
        var dirty = "Ada\u2028Sub\u2029scr\u202Eiber\u200B\uFEFF" + char.ConvertFromUtf32(0xE0041) + "\nX";

        await h.SubscribeAsync(dirty, Email);

        var stored = Assert.Single(await h.StoredAsync());
        Assert.Equal("AdaSubscriberX", stored.Name);
        Assert.Equal("AdaSubscriberX", h.Factory.Mail.Rendered.Single().To.Name);
    }
}
