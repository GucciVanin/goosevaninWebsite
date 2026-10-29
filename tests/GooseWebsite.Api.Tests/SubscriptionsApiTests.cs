using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GooseWebsite.Api.Modules.Subscriptions.Domain;
using GooseWebsite.Api.Modules.Subscriptions.Models;
using GooseWebsite.Api.Modules.Subscriptions.Services;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F10-US1: a visitor subscribes with a name and an email, confirms through the emailed link, and no response
// ever reveals whether an address is already subscribed. Mail goes to the test mail double, never to a provider.
public sealed class SubscriptionsApiTests
{
    private const string Email = "ada.subscriber@example.test";
    private const string SubscriberName = "Ada Subscriber";

    private sealed class Harness : IDisposable
    {
        public Harness(int? permitLimit = null)
        {
            Clock = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-11-01T10:00:00Z"));
            Factory = new AuthApiFactory(configureServices: services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Clock);
                services.Configure<SubscriptionOptions>(options =>
                {
                    options.RateLimit.PermitLimit = permitLimit ?? 1000;
                    options.RateLimit.ConfirmPermitLimit = 1000;
                });
            });
            Client = Factory.CreateHttpsClient();
        }

        public AdjustableTimeProvider Clock { get; }

        public AuthApiFactory Factory { get; }

        public HttpClient Client { get; }

        public async Task<(HttpStatusCode Status, string Body)> SubscribeAsync(string name, string email, string website = "")
        {
            var response = await PostAsync("/api/subscriptions", new { name, email, website });
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        public async Task<(HttpStatusCode Status, string Body)> ConfirmAsync(string token)
        {
            var response = await PostAsync("/api/subscriptions/confirm", new { token });
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        public async Task<HttpResponseMessage> PostAsync(string path, object body, bool withCsrf = true)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
            if (withCsrf)
            {
                var csrf = (await (await Client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken;
                request.Headers.Add("X-CSRF-TOKEN", csrf);
            }

            return await Client.SendAsync(request);
        }

        public async Task<List<Subscriber>> StoredAsync()
        {
            using var scope = Factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<DatabaseService>().Read<Subscriber>().OrderBy(s => s.Id).ToListAsync();
        }

        public string TokenFromLastMail()
        {
            var mail = Factory.Mail.Rendered.Last();
            var match = Regex.Match(mail.PlainText, @"/subscribe/confirm#token=([A-Za-z0-9_\-]+)");
            Assert.True(match.Success, "The confirmation mail carries the link to /subscribe/confirm with the token in the fragment.");
            return match.Groups[1].Value;
        }

        public void Dispose()
        {
            Client.Dispose();
            Factory.Dispose();
        }
    }

    [Fact]
    public async Task Subscribe_StoresAPendingSubscriberAndSendsOneConfirmationEmail()
    {
        using var h = new Harness();

        var (status, _) = await h.SubscribeAsync(SubscriberName, Email);

        Assert.Equal(HttpStatusCode.OK, status);
        var mail = Assert.Single(h.Factory.Mail.Rendered);
        Assert.Equal(Email, mail.To.Email);
        Assert.Equal(SubscriberName, mail.To.Name);
        var stored = Assert.Single(await h.StoredAsync());
        Assert.Equal(SubscriberState.Pending, stored.State);
        Assert.Equal(SubscriberName, stored.Name);
        Assert.Equal(Email, stored.Email);
        Assert.Null(stored.ConfirmedAtUtc);
        Assert.Equal(h.Clock.GetUtcNow().UtcDateTime, stored.CreatedAtUtc);
    }

    [Fact]
    public async Task EveryKindOfSubmissionIsAnsweredIdentically()
    {
        using var h = new Harness();
        var fresh = await h.SubscribeAsync(SubscriberName, Email);
        var token = h.TokenFromLastMail();
        var pendingAgain = await h.SubscribeAsync(SubscriberName, Email);
        Assert.Equal(HttpStatusCode.OK, (await h.ConfirmAsync(token)).Status);
        var confirmed = await h.SubscribeAsync(SubscriberName, Email);
        var invalidEmail = await h.SubscribeAsync(SubscriberName, "not-an-email");
        var emptyName = await h.SubscribeAsync("   ", "other@example.test");
        var longName = await h.SubscribeAsync(new string('x', 101), "long@example.test");
        var honeypot = await h.SubscribeAsync(SubscriberName, "bot@example.test", website: "https://spam.example");

        foreach (var other in new[] { pendingAgain, confirmed, invalidEmail, emptyName, longName, honeypot })
        {
            Assert.Equal(fresh.Status, other.Status);
            Assert.Equal(fresh.Body, other.Body);
        }

        Assert.DoesNotContain(Email, fresh.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Confirm_MakesTheSubscriberConfirmed_AndTheLinkWorksOnlyOnce()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var token = h.TokenFromLastMail();
        h.Clock.Advance(TimeSpan.FromHours(1));

        var first = await h.ConfirmAsync(token);
        var stored = Assert.Single(await h.StoredAsync());

        Assert.Equal(HttpStatusCode.OK, first.Status);
        Assert.Equal(SubscriberState.Confirmed, stored.State);
        Assert.Equal(h.Clock.GetUtcNow().UtcDateTime, stored.ConfirmedAtUtc);
        Assert.Null(stored.ConfirmationTokenHash);
        Assert.Null(stored.ConfirmationTokenExpiresAtUtc);

        var reuse = await h.ConfirmAsync(token);

        Assert.Equal(HttpStatusCode.BadRequest, reuse.Status);
        Assert.Equal(stored.ConfirmedAtUtc, Assert.Single(await h.StoredAsync()).ConfirmedAtUtc);
    }

    [Fact]
    public async Task Confirm_RefusesAnExpiredLinkAndChangesNothing()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var token = h.TokenFromLastMail();
        h.Clock.Advance(TimeSpan.FromDays(7) + TimeSpan.FromMinutes(1));

        var result = await h.ConfirmAsync(token);

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(SubscriberState.Pending, Assert.Single(await h.StoredAsync()).State);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-real-token")]
    [InlineData("%%%%")]
    public async Task Confirm_RefusesMalformedOrUnknownTokensWithTheSameAnswerAndChangesNothing(string token)
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var reused = await h.ConfirmAsync("zzzz-unknown");

        var result = await h.ConfirmAsync(token);

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(reused.Body, result.Body);
        Assert.Equal(SubscriberState.Pending, Assert.Single(await h.StoredAsync()).State);
    }

    [Fact]
    public async Task OpeningTheConfirmationEndpointWithGet_NeverConfirms()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var token = h.TokenFromLastMail();

        await h.Client.GetAsync($"/api/subscriptions/confirm?token={token}");

        Assert.Equal(SubscriberState.Pending, Assert.Single(await h.StoredAsync()).State);
    }

    [Fact]
    public async Task ResubmittingAPendingAddress_SendsAFreshLinkWithoutASecondRecord_AndTheOldLinkStopsWorking()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var oldToken = h.TokenFromLastMail();
        h.Clock.Advance(TimeSpan.FromMinutes(30));

        await h.SubscribeAsync("Ada Renamed", Email);

        Assert.Equal(2, h.Factory.Mail.Rendered.Count);
        var newToken = h.TokenFromLastMail();
        Assert.NotEqual(oldToken, newToken);
        var stored = Assert.Single(await h.StoredAsync());
        Assert.Equal(SubscriberState.Pending, stored.State);
        Assert.Equal(h.Clock.GetUtcNow().UtcDateTime.AddDays(7), stored.ConfirmationTokenExpiresAtUtc);
        Assert.Equal(HttpStatusCode.BadRequest, (await h.ConfirmAsync(oldToken)).Status);
        Assert.Equal(HttpStatusCode.OK, (await h.ConfirmAsync(newToken)).Status);
    }

    [Fact]
    public async Task ResubmittingAPendingAddressAgainAtOnce_DoesNotSendAnotherMail()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);

        await h.SubscribeAsync(SubscriberName, Email);
        await h.SubscribeAsync(SubscriberName, Email.ToUpperInvariant());

        Assert.Single(h.Factory.Mail.Rendered);
        Assert.Single(await h.StoredAsync());
    }

    [Fact]
    public async Task ASecondSubmissionOfAConfirmedAddress_SendsNoMailAndChangesNothing()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        await h.ConfirmAsync(h.TokenFromLastMail());
        h.Clock.Advance(TimeSpan.FromHours(1));

        await h.SubscribeAsync("Someone Else", Email);

        Assert.Single(h.Factory.Mail.Rendered);
        var stored = Assert.Single(await h.StoredAsync());
        Assert.Equal(SubscriberState.Confirmed, stored.State);
        Assert.Equal(SubscriberName, stored.Name);
    }

    [Fact]
    public async Task TheEmailAddressIsUniqueIgnoringCase()
    {
        using var h = new Harness();

        await h.SubscribeAsync(SubscriberName, "Mixed.Case@Example.test");
        await h.SubscribeAsync(SubscriberName, "mixed.case@example.test");

        Assert.Single(await h.StoredAsync());
        using var scope = h.Factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        database.Add(new Subscriber { Name = "Dup", Email = "MIXED.CASE@EXAMPLE.TEST", State = SubscriberState.Pending, CreatedAtUtc = DateTime.UtcNow, UnsubscribeTokenHash = "x" });
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("a@b")]
    [InlineData("Name <a@b.test>")]
    [InlineData("two@example.test, three@example.test")]
    [InlineData("with space@example.test")]
    public async Task AnInvalidEmailStoresNothingAndSendsNothing(string email)
    {
        using var h = new Harness();

        var (status, _) = await h.SubscribeAsync(SubscriberName, email);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Empty(await h.StoredAsync());
        Assert.Empty(h.Factory.Mail.Rendered);
    }

    [Fact]
    public async Task AnEmailOver254CharactersOrANameOver100IsNotStored()
    {
        using var h = new Harness();
        var longEmail = new string('a', 250) + "@example.test";

        await h.SubscribeAsync(SubscriberName, longEmail);
        await h.SubscribeAsync(new string('n', 101), Email);

        Assert.Empty(await h.StoredAsync());
        Assert.Empty(h.Factory.Mail.Rendered);
    }

    [Fact]
    public async Task AFilledHoneypotStoresNothingAndSendsNothing()
    {
        using var h = new Harness();

        await h.SubscribeAsync(SubscriberName, Email, website: "http://spam.example");

        Assert.Empty(await h.StoredAsync());
        Assert.Empty(h.Factory.Mail.Rendered);
    }

    [Fact]
    public async Task ExceedingThePerIpLimitStoresNothingMoreAndSendsNothingMore()
    {
        using var h = new Harness(permitLimit: 2);
        await h.SubscribeAsync(SubscriberName, "one@example.test");
        await h.SubscribeAsync(SubscriberName, "two@example.test");

        var third = await h.SubscribeAsync(SubscriberName, "three@example.test");

        Assert.Equal(HttpStatusCode.TooManyRequests, third.Status);
        Assert.Equal(2, (await h.StoredAsync()).Count);
        Assert.Equal(2, h.Factory.Mail.Rendered.Count);
    }

    [Fact]
    public async Task ASubmissionWithoutTheCsrfTokenIsRefused()
    {
        using var h = new Harness();

        var response = await h.PostAsync("/api/subscriptions", new { name = SubscriberName, email = Email, website = "" }, withCsrf: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await h.StoredAsync());
        Assert.Empty(h.Factory.Mail.Rendered);
    }

    [Fact]
    public async Task WhenTheMailProviderFails_TheAnswerIsTheSameAndTheRecordStaysPending()
    {
        using var h = new Harness();
        var ok = await h.SubscribeAsync(SubscriberName, "ok@example.test");
        h.Factory.Mail.ShouldFail = true;

        var failed = await h.SubscribeAsync(SubscriberName, Email);

        Assert.Equal(ok.Status, failed.Status);
        Assert.Equal(ok.Body, failed.Body);
        Assert.Contains(await h.StoredAsync(), s => s.Email == Email && s.State == SubscriberState.Pending);
    }

    [Fact]
    public async Task TheStoredRecordHoldsOnlyTheDocumentedColumns_AndTokensAreOnlyHashed()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var token = h.TokenFromLastMail();

        using var scope = h.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var connection = (SqliteConnection)context.Database.GetDbConnection();
        var columns = new List<string>();
        var values = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM pragma_table_info('Subscribers')";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM Subscribers";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    values.Add(Convert.ToString(reader.GetValue(i)) ?? string.Empty);
                }
            }
        }

        Assert.Equal(
            new[]
            {
                "ConfirmationTokenExpiresAtUtc", "ConfirmationTokenHash", "ConfirmedAtUtc", "CreatedAtUtc",
                "Email", "Id", "Name", "State", "UnsubscribeTokenHash"
            },
            columns.OrderBy(name => name, StringComparer.Ordinal).ToArray());
        Assert.DoesNotContain(values, value => value.Contains(token, StringComparison.Ordinal));
        var stored = Assert.Single(await h.StoredAsync());
        Assert.Matches("^[0-9A-F]{64}$", stored.ConfirmationTokenHash);
        Assert.Matches("^[0-9A-F]{64}$", stored.UnsubscribeTokenHash);
    }

    [Fact]
    public void TheAccessRuleLimitsTheNameTo100AndTheEmailTo254AndTreatsThemAsUserText()
    {
        var rule = AccessRuleCatalog.RuleFor(typeof(Subscriber));

        Assert.NotNull(rule);
        Assert.Equal(100, rule!.UserText.Single(text => text.Property.Name == nameof(Subscriber.Name)).MaxLength);
        Assert.Equal(254, rule.UserText.Single(text => text.Property.Name == nameof(Subscriber.Email)).MaxLength);
    }

    [Fact]
    public async Task TheSafeguardsRefuseAnOverLongNameBeneathTheService()
    {
        using var h = new Harness();
        using var scope = h.Factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        database.Add(new Subscriber { Name = new string('n', 101), Email = Email, State = SubscriberState.Pending, CreatedAtUtc = DateTime.UtcNow, UnsubscribeTokenHash = "x" });

        await Assert.ThrowsAsync<DataRejectedException>(() => database.SaveChangesAsync());
    }

    [Fact]
    public async Task NeitherTheNameNorTheEmailNorTheTokenIsEverLogged()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);
        var token = h.TokenFromLastMail();
        await h.SubscribeAsync(SubscriberName, Email);
        h.Factory.Mail.ShouldFail = true;
        h.Clock.Advance(TimeSpan.FromHours(1));
        await h.SubscribeAsync("Grace Failing", "grace.failing@example.test");
        h.Factory.Mail.ShouldFail = false;
        await h.ConfirmAsync(token);
        await h.ConfirmAsync("an-unknown-token-value");

        var log = string.Join('\n', h.Factory.Logs.Messages);

        foreach (var secret in new[] { Email, "ada.subscriber", SubscriberName, "Ada", "grace.failing", "Grace Failing", token, "an-unknown-token-value" })
        {
            Assert.DoesNotContain(secret, log, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task NoEndpointListsSubscribers()
    {
        using var h = new Harness();
        await h.SubscribeAsync(SubscriberName, Email);

        foreach (var path in new[] { "/api/subscriptions", "/api/subscriptions/confirm", "/api/subscriptions/list", "/api/editor/subscriptions" })
        {
            var response = await h.Client.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(Email, body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(SubscriberName, body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task TheConfirmationEmailIsBrandedEncodesTheNameAndLinksToTheClientPage()
    {
        using var h = new Harness();

        await h.SubscribeAsync("<b>Ada</b> & Co", Email);

        var mail = Assert.Single(h.Factory.Mail.Rendered);
        Assert.Equal("Confirm your subscription", mail.Subject);
        Assert.Contains("https://localhost/subscribe/confirm#token=", mail.PlainText);
        Assert.Contains("https://localhost/subscribe/confirm#token=", mail.Html);
        Assert.DoesNotContain("<b>Ada</b>", mail.Html, StringComparison.Ordinal);
        Assert.Contains("&lt;b&gt;Ada&lt;/b&gt; &amp; Co", mail.Html, StringComparison.Ordinal);
        Assert.Contains("#05060f", mail.Html);
        Assert.DoesNotContain("<script", mail.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ignore this message", mail.PlainText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CleanupDeletesPendingRecordsWhoseLinkExpiredAndKeepsEverythingElse()
    {
        using var h = new Harness();
        await h.SubscribeAsync("Old Pending", "old@example.test");
        await h.SubscribeAsync("Old Confirmed", "oldconfirmed@example.test");
        await h.ConfirmAsync(h.TokenFromLastMail());
        h.Clock.Advance(TimeSpan.FromDays(6));
        await h.SubscribeAsync("Recent Pending", "recent@example.test");
        h.Clock.Advance(TimeSpan.FromDays(1) + TimeSpan.FromMinutes(5));

        using var scope = h.Factory.Services.CreateScope();
        var removed = await scope.ServiceProvider.GetRequiredService<ISubscriptionService>().RemoveExpiredPendingAsync();

        Assert.Equal(1, removed);
        var emails = (await h.StoredAsync()).Select(s => s.Email).ToArray();
        Assert.Equal(["oldconfirmed@example.test", "recent@example.test"], emails);
    }

    [Fact]
    public async Task TheCleanupJobIsRegisteredAsAHostedService()
    {
        using var h = new Harness();

        Assert.Contains(h.Factory.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>(), service => service is PendingSubscriberCleanupService);
        await Task.CompletedTask;
    }
}
