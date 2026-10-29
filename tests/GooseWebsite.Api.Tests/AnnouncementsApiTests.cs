using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GooseWebsite.Api.Modules.Blog.Domain;
using GooseWebsite.Api.Modules.Projects.Domain;
using GooseWebsite.Api.Modules.Subscriptions.Domain;
using GooseWebsite.Api.Modules.Subscriptions.Models;
using GooseWebsite.Api.Modules.Subscriptions.Services;
using GooseWebsite.Api.Shared.Announcements;
using GooseWebsite.Api.Shared.Email;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F10-US3: the first time Gustavo publishes a story or a project, every confirmed subscriber gets one email with
// its title, summary and link. Sending happens in the background (the tests run a round by hand, with the poll
// switched off), the publish never fails because of the mail provider, and recipients not yet reached are retried
// without mailing anyone twice. Mail goes to the test mail double, never to a provider.
public sealed class AnnouncementsApiTests
{
    private const string AdminEmail = "editor-admin@example.test";
    private const string Password = "Strong-password-123";
    private const string Ada = "ada.reader@example.test";
    private const string Grace = "grace.reader@example.test";

    private sealed class Site : IDisposable
    {
        private Site(AuthApiFactory factory, HttpClient admin, AdjustableTimeProvider clock)
        {
            Factory = factory;
            Admin = admin;
            Clock = clock;
        }

        public AuthApiFactory Factory { get; }

        public HttpClient Admin { get; }

        public AdjustableTimeProvider Clock { get; }

        public TestMailService Mail => Factory.Mail;

        public IAnnouncementDispatcher Dispatcher => Factory.Services.GetRequiredService<IAnnouncementDispatcher>();

        public static async Task<Site> StartAsync(Action<SubscriptionOptions>? configure = null, Action<IServiceCollection>? configureServices = null)
        {
            var clock = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-11-01T10:00:00Z"));
            var factory = new AuthApiFactory(configureServices: services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(clock);
                configureServices?.Invoke(services);
                services.Configure<SubscriptionOptions>(options =>
                {
                    // The hosted delivery loop stays asleep; tests run a delivery round by hand unless they say otherwise.
                    options.Announcements.PollSeconds = 3600;
                    configure?.Invoke(options);
                });
            });
            await factory.ProvisionAdministratorAsync(AdminEmail, Password);
            var client = factory.CreateHttpsClient();
            var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new { email = AdminEmail, password = Password }) };
            login.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(login)).StatusCode);
            return new Site(factory, client, clock);
        }

        private static async Task<string> CsrfAsync(HttpClient client) =>
            (await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken;

        public async Task<(HttpStatusCode Status, string Text, JsonElement Body)> SendAsync(HttpMethod method, string path, object? body = null)
        {
            var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(Admin));
            var response = await Admin.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            return (response.StatusCode, text, text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone());
        }

        public async Task<int> AddSubscriberAsync(string name, string email, SubscriberState state = SubscriberState.Confirmed)
        {
            using var scope = Factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
            var subscriber = new Subscriber
            {
                Name = name,
                Email = email,
                State = state,
                CreatedAtUtc = Clock.GetUtcNow().UtcDateTime,
                ConfirmedAtUtc = state == SubscriberState.Confirmed ? Clock.GetUtcNow().UtcDateTime : null
            };
            database.Add(subscriber);
            await database.SaveChangesAsync();
            return subscriber.Id;
        }

        public async Task ConfirmAsync(int subscriberId)
        {
            using var scope = Factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
            var subscriber = await database.ReadForUpdate<Subscriber>().SingleAsync(candidate => candidate.Id == subscriberId);
            subscriber.State = SubscriberState.Confirmed;
            subscriber.ConfirmedAtUtc = Clock.GetUtcNow().UtcDateTime;
            await database.SaveChangesAsync();
        }

        public async Task RemoveSubscriberAsync(int subscriberId)
        {
            using var scope = Factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
            database.Remove(await database.ReadForUpdate<Subscriber>().SingleAsync(candidate => candidate.Id == subscriberId));
            await database.SaveChangesAsync();
        }

        public async Task<(int Id, string Slug)> CreateStoryAsync(string title = "A first story", string excerpt = "Why it matters.", string content = "Some text.")
        {
            var created = await SendAsync(HttpMethod.Post, "/api/editor/stories", new { title, excerpt, content, category = "Notes", tags = new[] { "life" } });
            Assert.Equal(HttpStatusCode.Created, created.Status);
            return (created.Body.GetProperty("id").GetInt32(), created.Body.GetProperty("slug").GetString()!);
        }

        public Task<(HttpStatusCode Status, string Text, JsonElement Body)> PublishStoryAsync(int id) =>
            SendAsync(HttpMethod.Post, $"/api/editor/stories/{id}/publish");

        public Task<(HttpStatusCode Status, string Text, JsonElement Body)> SaveStoryAsync(int id, string title, string excerpt, string content = "Edited text.") =>
            SendAsync(HttpMethod.Put, $"/api/editor/stories/{id}", new { title, excerpt, content, category = "Notes", tags = new[] { "life" } });

        public async Task<(int Id, string Slug)> CreateProjectAsync(string title = "Goose website", string overview = "A personal site.")
        {
            var created = await SendAsync(HttpMethod.Post, "/api/editor/projects", new { title, overview, contribution = "Everything.", technologies = new[] { "C#" } });
            Assert.Equal(HttpStatusCode.Created, created.Status);
            return (created.Body.GetProperty("id").GetInt32(), created.Body.GetProperty("slug").GetString()!);
        }

        public Task<(HttpStatusCode Status, string Text, JsonElement Body)> PublishProjectAsync(int id) =>
            SendAsync(HttpMethod.Post, $"/api/editor/projects/{id}/publish");

        public async Task<List<T>> ReadAsync<T>() where T : class
        {
            using var scope = Factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<DatabaseService>().Read<T>().ToListAsync();
        }

        public void Dispose()
        {
            Admin.Dispose();
            Factory.Dispose();
        }
    }

    // ---- AC1: first publish, one email per confirmed subscriber, none to pending ones ----

    [Fact]
    public async Task TheFirstPublishOfAStoryMailsEachConfirmedSubscriberOnce_AndNoPendingOne()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        await site.AddSubscriberAsync("Grace Reader", Grace);
        await site.AddSubscriberAsync("Pending Person", "pending.person@example.test", SubscriberState.Pending);
        var (id, slug) = await site.CreateStoryAsync("The goose story", "A short summary of the goose story.");

        var published = await site.PublishStoryAsync(id);
        var round = await site.Dispatcher.DeliverPendingAsync();
        var again = await site.Dispatcher.DeliverPendingAsync();

        Assert.Equal(HttpStatusCode.OK, published.Status);
        Assert.Equal(2, round.Sent);
        Assert.Equal(0, again.Sent);
        var mails = site.Mail.Rendered;
        Assert.Equal(2, mails.Count);
        Assert.Equal([Ada, Grace], mails.Select(mail => mail.To.Email).OrderBy(email => email).ToArray());
        Assert.DoesNotContain(mails, mail => mail.To.Email.StartsWith("pending", StringComparison.Ordinal));
        foreach (var mail in mails)
        {
            Assert.Contains("The goose story", mail.Subject);
            Assert.Contains("The goose story", mail.PlainText);
            Assert.Contains("A short summary of the goose story.", mail.PlainText);
            Assert.Contains($"https://localhost/stories/{slug}", mail.PlainText);
            Assert.Contains("The goose story", mail.Html);
            Assert.Contains("A short summary of the goose story.", mail.Html);
            Assert.Contains($"href=\"https://localhost/stories/{slug}\"", mail.Html);
        }
    }

    [Fact]
    public async Task TheFirstPublishOfAProjectMailsEachConfirmedSubscriberOnceWithTheOverviewAsSummary()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (id, slug) = await site.CreateProjectAsync("Goose website", "A **personal** site with [links](https://example.test) and\n\n# a heading.");

        Assert.Equal(HttpStatusCode.OK, (await site.PublishProjectAsync(id)).Status);
        await site.Dispatcher.DeliverPendingAsync();

        var mail = Assert.Single(site.Mail.Rendered);
        Assert.Equal(Ada, mail.To.Email);
        Assert.Contains("Goose website", mail.Subject);
        Assert.Contains($"https://localhost/projects/{slug}", mail.PlainText);
        Assert.Contains("A personal site with links and a heading.", mail.PlainText);
        Assert.DoesNotContain("**", mail.PlainText);
        Assert.DoesNotContain("](", mail.PlainText);
    }

    [Fact]
    public async Task EveryMailIsPersonal_ItsOwnGreetingUnsubscribeLinkAndHeaders()
    {
        using var site = await Site.StartAsync();
        var adaId = await site.AddSubscriberAsync("Ada Reader", Ada);
        var graceId = await site.AddSubscriberAsync("Grace Reader", Grace);
        var (id, _) = await site.CreateStoryAsync();
        await site.PublishStoryAsync(id);

        await site.Dispatcher.DeliverPendingAsync();

        using var scope = site.Factory.Services.CreateScope();
        var links = scope.ServiceProvider.GetRequiredService<ISubscriptionLinks>();
        foreach (var (email, name, subscriberId) in new[] { (Ada, "Ada Reader", adaId), (Grace, "Grace Reader", graceId) })
        {
            var mail = site.Mail.Rendered.Single(candidate => candidate.To.Email == email);
            Assert.Equal(name, mail.To.Name);
            Assert.Contains($"Hi {name}", mail.PlainText);
            var link = Regex.Match(mail.PlainText, @"https://localhost/unsubscribe#token=([A-Za-z0-9_\-%.]+)");
            Assert.True(link.Success, "The body has the personal unsubscribe link.");
            Assert.True(links.TryReadSubscriberId(Uri.UnescapeDataString(link.Groups[1].Value), out var linkId));
            Assert.Equal(subscriberId, linkId);
            Assert.Contains("https://localhost/unsubscribe#token=", mail.Html);
            Assert.NotNull(mail.Headers);
            var header = Regex.Match(mail.Headers!["List-Unsubscribe"], @"^<https://localhost/api/subscriptions/unsubscribe/one-click\?token=([A-Za-z0-9_\-%.]+)>$");
            Assert.True(header.Success);
            Assert.True(links.TryReadSubscriberId(Uri.UnescapeDataString(header.Groups[1].Value), out var headerId));
            Assert.Equal(subscriberId, headerId);
            Assert.Equal("List-Unsubscribe=One-Click", mail.Headers["List-Unsubscribe-Post"]);
        }
    }

    // ---- AC2: announced once ----

    [Fact]
    public async Task EditingRepublishingAndPublishingAgainNeverMailAgain_AndTheMarkerIsSetOnce()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (id, _) = await site.CreateStoryAsync();
        await site.PublishStoryAsync(id);
        await site.Dispatcher.DeliverPendingAsync();
        var marker = Assert.Single(await site.ReadAsync<BlogPost>()).AnnouncedAtUtc;
        Assert.Equal(site.Clock.GetUtcNow().UtcDateTime, marker);
        Assert.Single(site.Mail.Rendered);
        site.Clock.Advance(TimeSpan.FromMinutes(5));

        await site.PublishStoryAsync(id);
        await site.SaveStoryAsync(id, "A changed title", "A changed excerpt.");
        await site.SendAsync(HttpMethod.Post, $"/api/editor/stories/{id}/unpublish");
        await site.SaveStoryAsync(id, "A changed title again", "Changed again.");
        var republished = await site.PublishStoryAsync(id);
        await site.Dispatcher.DeliverPendingAsync();

        Assert.Equal(HttpStatusCode.OK, republished.Status);
        Assert.Single(site.Mail.Rendered);
        Assert.Equal(marker, Assert.Single(await site.ReadAsync<BlogPost>()).AnnouncedAtUtc);
        Assert.Single(await site.ReadAsync<Announcement>());
    }

    [Fact]
    public async Task AProjectIsAnnouncedOnlyOnItsFirstPublishToo()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (id, _) = await site.CreateProjectAsync();
        await site.PublishProjectAsync(id);
        await site.SendAsync(HttpMethod.Post, $"/api/editor/projects/{id}/unpublish");
        await site.PublishProjectAsync(id);
        await site.Dispatcher.DeliverPendingAsync();

        Assert.Single(site.Mail.Rendered);
        Assert.NotNull(Assert.Single(await site.ReadAsync<Project>()).AnnouncedAtUtc);
    }

    [Fact]
    public async Task APublishThatIsRefusedAnnouncesNothing_AndThePublishThatFollowsDoes()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (id, _) = await site.CreateStoryAsync("Incomplete", excerpt: "", content: "");

        var refused = await site.PublishStoryAsync(id);
        await site.Dispatcher.DeliverPendingAsync();

        Assert.Equal(HttpStatusCode.BadRequest, refused.Status);
        Assert.Empty(site.Mail.Rendered);
        Assert.Null(Assert.Single(await site.ReadAsync<BlogPost>()).AnnouncedAtUtc);
        await site.SaveStoryAsync(id, "Incomplete", "Now complete.", "A body.");
        Assert.Equal(HttpStatusCode.OK, (await site.PublishStoryAsync(id)).Status);
        await site.Dispatcher.DeliverPendingAsync();
        Assert.Single(site.Mail.Rendered);
    }

    [Fact]
    public async Task ContentPublishedBeforeSubscriptionsExistedIsMarkedAsAnnounced_SoRepublishingItStaysSilent()
    {
        // The migration marks every story and project that was ever published; this checks the same SQL on a database
        // that has the old shape (no marker column), filled before the migration ran.
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        var migrator = context.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        await migrator.MigrateAsync("DropSubscriberUnsubscribeTokenHash");
        await using (var seed = connection.CreateCommand())
        {
            seed.CommandText = """
                INSERT INTO AspNetUsers (Id, AccessFailedCount, EmailConfirmed, LockoutEnabled, PhoneNumberConfirmed, TwoFactorEnabled, DisplayName)
                VALUES ('u1', 0, 1, 0, 0, 0, 'Gustavo');
                INSERT INTO BlogPosts (Title, Slug, Excerpt, Content, Category, Tags, CoverImage, CoverImageAlt, IsPublished, CreatedAtUtc, UpdatedAtUtc, PublishedAtUtc, AuthorId) VALUES
                  ('Live', 'live', 'e', 'c', 'Notes', '', '', '', 1, '2024-01-02 00:00:00', '2024-01-02 00:00:00', '2024-01-03 00:00:00', 'u1'),
                  ('Pulled back', 'pulled', 'e', 'c', 'Notes', '', '', '', 0, '2024-01-02 00:00:00', '2024-01-02 00:00:00', '2024-01-03 00:00:00', 'u1'),
                  ('Never published', 'never', 'e', 'c', 'Notes', '', '', '', 0, '2024-01-02 00:00:00', '2024-01-02 00:00:00', NULL, 'u1');
                INSERT INTO Projects (Title, Slug, Overview, Contribution, Technologies, CoverImage, CoverImageAlt, IsPublished, CreatedAtUtc, UpdatedAtUtc, PublishedAtUtc, AuthorId) VALUES
                  ('Live project', 'live-project', 'o', 'c', '', '', '', 1, '2024-01-02 00:00:00', '2024-01-02 00:00:00', NULL, 'u1'),
                  ('Draft project', 'draft-project', 'o', 'c', '', '', '', 0, '2024-01-02 00:00:00', '2024-01-02 00:00:00', NULL, 'u1');
                """;
            await seed.ExecuteNonQueryAsync();
        }

        await migrator.MigrateAsync();

        await using var read = connection.CreateCommand();
        read.CommandText = "SELECT Slug, AnnouncedAtUtc IS NOT NULL FROM BlogPosts UNION ALL SELECT Slug, AnnouncedAtUtc IS NOT NULL FROM Projects ORDER BY 1";
        var marked = new Dictionary<string, bool>();
        await using (var reader = await read.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                marked[reader.GetString(0)] = reader.GetInt64(1) == 1;
            }
        }

        Assert.True(marked["live"]);
        Assert.True(marked["pulled"], "A story that was published once and pulled back counts as announced.");
        Assert.False(marked["never"]);
        Assert.True(marked["live-project"]);
        Assert.False(marked["draft-project"]);
    }

    // ---- AC3: drafts ----

    [Fact]
    public async Task SavingDraftsAndEditingThemSendsNothingAndQueuesNothing()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (storyId, _) = await site.CreateStoryAsync();
        await site.SaveStoryAsync(storyId, "Edited", "Edited excerpt.");
        var (projectId, _) = await site.CreateProjectAsync();
        await site.SendAsync(HttpMethod.Put, $"/api/editor/projects/{projectId}", new { title = "Edited project", overview = "Edited." });

        await site.Dispatcher.DeliverPendingAsync();

        Assert.Empty(site.Mail.Rendered);
        Assert.Empty(await site.ReadAsync<Announcement>());
        Assert.Empty(await site.ReadAsync<AnnouncementDelivery>());
    }

    // ---- AC4: the provider never fails the publish; retries never duplicate ----

    [Fact]
    public async Task PublishingReturnsBeforeAnyMailIsSent_AndAProviderThatFailsEverythingNeverFailsThePublish()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        site.Mail.ShouldFail = true;
        var (id, slug) = await site.CreateStoryAsync();

        var published = await site.PublishStoryAsync(id);

        Assert.Equal(HttpStatusCode.OK, published.Status);
        Assert.True(published.Body.GetProperty("isPublished").GetBoolean());
        Assert.Empty(site.Mail.Rendered);
        using var anonymous = site.Factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"/api/blog/{slug}")).StatusCode);

        var round = await site.Dispatcher.DeliverPendingAsync();

        Assert.Equal(0, round.Sent);
        Assert.Equal(1, round.Failed);
        Assert.Contains(site.Factory.Logs.Messages, message => message.Contains("Announcement delivery round", StringComparison.Ordinal));
    }

    [Fact]
    public async Task APartialFailureIsRetriedLaterWithoutMailingTheRecipientsAlreadyReached()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        await site.AddSubscriberAsync("Grace Reader", Grace);
        site.Mail.FailRecipients.Add(Grace);
        var (id, _) = await site.CreateStoryAsync();
        await site.PublishStoryAsync(id);

        var first = await site.Dispatcher.DeliverPendingAsync();
        Assert.Equal((1, 1), (first.Sent, first.Failed));
        Assert.Equal([Ada], site.Mail.Rendered.Select(mail => mail.To.Email).ToArray());

        // Right away nothing is retried: a failed recipient waits for the retry delay.
        var tooSoon = await site.Dispatcher.DeliverPendingAsync();
        Assert.Equal((0, 0), (tooSoon.Sent, tooSoon.Failed));

        site.Clock.Advance(TimeSpan.FromMinutes(11));
        var stillDown = await site.Dispatcher.DeliverPendingAsync();
        Assert.Equal((0, 1), (stillDown.Sent, stillDown.Failed));

        site.Mail.FailRecipients.Clear();
        site.Clock.Advance(TimeSpan.FromMinutes(30));
        var recovered = await site.Dispatcher.DeliverPendingAsync();
        var after = await site.Dispatcher.DeliverPendingAsync();

        Assert.Equal((1, 0), (recovered.Sent, recovered.Failed));
        Assert.Equal((0, 0), (after.Sent, after.Failed));
        Assert.Equal([Ada, Grace], site.Mail.Rendered.Select(mail => mail.To.Email).OrderBy(email => email).ToArray());
        Assert.All(await site.ReadAsync<AnnouncementDelivery>(), delivery => Assert.NotNull(delivery.SentAtUtc));
        Assert.NotNull(Assert.Single(await site.ReadAsync<Announcement>()).CompletedAtUtc);
    }

    [Fact]
    public async Task ARecipientThatKeepsFailingIsGivenUpAfterTheConfiguredAttempts_AndTheAnnouncementCloses()
    {
        using var site = await Site.StartAsync(options => options.Announcements.MaxAttempts = 3);
        await site.AddSubscriberAsync("Ada Reader", Ada);
        site.Mail.ShouldFail = true;
        var (id, _) = await site.CreateStoryAsync();
        await site.PublishStoryAsync(id);

        var failures = 0;
        for (var attempt = 0; attempt < 6; attempt++)
        {
            failures += (await site.Dispatcher.DeliverPendingAsync()).Failed;
            site.Clock.Advance(TimeSpan.FromHours(2));
        }

        Assert.Equal(3, failures);
        Assert.Equal(3, Assert.Single(await site.ReadAsync<AnnouncementDelivery>()).Attempts);
        Assert.NotNull(Assert.Single(await site.ReadAsync<Announcement>()).CompletedAtUtc);
    }

    [Fact]
    public async Task TheHostedLoopDeliversInTheBackgroundWithoutTheEditorRequestWaiting()
    {
        using var site = await Site.StartAsync(options => options.Announcements.PollSeconds = 1);
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (id, _) = await site.CreateStoryAsync();

        await site.PublishStoryAsync(id);

        for (var waited = 0; waited < 100 && site.Mail.Rendered.Count == 0; waited++)
        {
            await Task.Delay(100);
        }

        Assert.Single(site.Mail.Rendered);
    }

    // ---- AC5: privacy ----

    [Fact]
    public async Task NeitherThePublishResponseNorTheLogsNorTheQueueTablesHoldSubscriberNamesOrAddresses()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        await site.AddSubscriberAsync("Grace Reader", Grace);
        site.Mail.FailRecipients.Add(Grace);
        var (id, _) = await site.CreateStoryAsync();

        var published = await site.PublishStoryAsync(id);
        await site.Dispatcher.DeliverPendingAsync();
        site.Clock.Advance(TimeSpan.FromMinutes(11));
        await site.Dispatcher.DeliverPendingAsync();
        var projectId = (await site.CreateProjectAsync()).Id;
        var publishedProject = await site.PublishProjectAsync(projectId);

        var secrets = new[] { "Ada Reader", "Grace Reader", "ada.reader", "grace.reader", "@example.test" };
        var log = string.Join('\n', site.Factory.Logs.Messages);
        foreach (var secret in secrets)
        {
            Assert.DoesNotContain(secret, published.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(secret, publishedProject.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(secret, log, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("subscriber", published.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("announce", published.Text, StringComparison.OrdinalIgnoreCase);

        using var scope = site.Factory.Services.CreateScope();
        var connection = (SqliteConnection)scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.GetDbConnection();
        foreach (var table in new[] { "Announcements", "AnnouncementDeliveries" })
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM {table}";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                for (var column = 0; column < reader.FieldCount; column++)
                {
                    var value = Convert.ToString(reader.GetValue(column)) ?? string.Empty;
                    foreach (var secret in new[] { "Ada Reader", "Grace Reader", "ada.reader", "grace.reader", "@example.test" })
                    {
                        Assert.DoesNotContain(secret, value, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }
    }

    [Fact]
    public async Task TheQueueKeepsOnlyIdsAndCounters()
    {
        using var site = await Site.StartAsync();
        using var scope = site.Factory.Services.CreateScope();
        var connection = (SqliteConnection)scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.GetDbConnection();

        async Task<string[]> ColumnsAsync(string table)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT name FROM pragma_table_info('{table}') ORDER BY cid";
            await using var reader = await command.ExecuteReaderAsync();
            var names = new List<string>();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }

            return names.ToArray();
        }

        Assert.Equal(
            ["Id", "AnnouncementId", "SubscriberId", "SentAtUtc", "Attempts", "LastAttemptAtUtc"],
            await ColumnsAsync("AnnouncementDeliveries"));
        Assert.DoesNotContain(await ColumnsAsync("Announcements"), name => name.Contains("Email", StringComparison.OrdinalIgnoreCase) || name.Contains("Name", StringComparison.Ordinal));
    }

    // ---- AC6: no subscribers ----

    [Fact]
    public async Task APublishWithNoSubscribersSendsNothingAndFailsNothing()
    {
        using var site = await Site.StartAsync();
        var (storyId, _) = await site.CreateStoryAsync();
        var (projectId, _) = await site.CreateProjectAsync();

        var story = await site.PublishStoryAsync(storyId);
        var project = await site.PublishProjectAsync(projectId);
        var round = await site.Dispatcher.DeliverPendingAsync();

        Assert.Equal(HttpStatusCode.OK, story.Status);
        Assert.Equal(HttpStatusCode.OK, project.Status);
        Assert.Equal((0, 0), (round.Sent, round.Failed));
        Assert.Empty(site.Mail.Rendered);
    }

    [Fact]
    public async Task OnlyPendingSubscribersMeansNothingIsSent()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Pending Person", "pending.person@example.test", SubscriberState.Pending);
        var (id, _) = await site.CreateStoryAsync();

        await site.PublishStoryAsync(id);
        await site.Dispatcher.DeliverPendingAsync();

        Assert.Empty(site.Mail.Rendered);
    }

    // ---- F10-US2 AC4 and the subscriber list changing around a publish ----

    [Fact]
    public async Task ASubscriberWhoUnsubscribedBeforeTheMailWasSentGetsNothing()
    {
        using var site = await Site.StartAsync();
        var adaId = await site.AddSubscriberAsync("Ada Reader", Ada);
        await site.AddSubscriberAsync("Grace Reader", Grace);
        var (id, _) = await site.CreateStoryAsync();
        await site.PublishStoryAsync(id);

        await site.RemoveSubscriberAsync(adaId);
        await site.Dispatcher.DeliverPendingAsync();

        Assert.Equal([Grace], site.Mail.Rendered.Select(mail => mail.To.Email).ToArray());
        Assert.Single(await site.ReadAsync<AnnouncementDelivery>());
    }

    [Fact]
    public async Task ASubscriberWhoConfirmsAfterThePublishIsNotToldAboutEarlierWork()
    {
        using var site = await Site.StartAsync();
        var lateId = await site.AddSubscriberAsync("Late Reader", "late.reader@example.test", SubscriberState.Pending);
        var (id, _) = await site.CreateStoryAsync();
        await site.PublishStoryAsync(id);
        await site.ConfirmAsync(lateId);

        await site.Dispatcher.DeliverPendingAsync();
        var (nextId, _) = await site.CreateStoryAsync("The next story", "Next.");
        await site.PublishStoryAsync(nextId);
        await site.Dispatcher.DeliverPendingAsync();

        var mail = Assert.Single(site.Mail.Rendered);
        Assert.Contains("The next story", mail.Subject);
    }

    // ---- The summary goes through the same public-text safety as the site ----

    [Fact]
    public async Task TheSummaryIsPlainEscapedAndShort_AndTheSubjectIsOneCleanLine()
    {
        using var site = await Site.StartAsync(options => options.Announcements.SummaryMaxLength = 120);
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var hostile = "<script>alert('x')</script> <img src=x onerror=alert(1)> & more‮​ text. " + new string('w', 20) + " " + string.Concat(Enumerable.Repeat("word ", 80));
        var (id, _) = await site.CreateStoryAsync("Title <b>bold</b>\r\nBcc: evil@example.test", hostile);

        await site.PublishStoryAsync(id);
        await site.Dispatcher.DeliverPendingAsync();

        var mail = Assert.Single(site.Mail.Rendered);
        Assert.DoesNotContain('\r', mail.Subject);
        Assert.DoesNotContain('\n', mail.Subject);
        Assert.DoesNotContain("<script", mail.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img src=x", mail.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<b>", mail.Html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", mail.Html);
        var everything = mail.Html + mail.PlainText + mail.Subject;
        Assert.False(everything.Contains((char)0x202E), "The right-to-left override is gone.");
        Assert.False(everything.Contains((char)0x200B), "The zero-width space is gone.");
        var summaryLine = mail.PlainText.Split('\n').Single(line => line.Contains("alert", StringComparison.Ordinal));
        Assert.True(summaryLine.Length <= 125, $"The summary is cut to the configured length; it was {summaryLine.Length}.");
        Assert.EndsWith("…", summaryLine.TrimEnd());
        Assert.DoesNotContain("evil@example.test", mail.Headers?.Values.FirstOrDefault(value => value.Contains("evil")) ?? string.Empty);
        Assert.DoesNotContain(mail.Headers!.Keys, key => key.Equals("Bcc", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AMissingSubscriberRecordOrAnAnnouncementOfDeletedContentNeverBreaksARound()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (id, _) = await site.CreateStoryAsync();
        await site.PublishStoryAsync(id);
        Assert.Equal(HttpStatusCode.NoContent, (await site.SendAsync(HttpMethod.Delete, $"/api/editor/stories/{id}")).Status);

        var round = await site.Dispatcher.DeliverPendingAsync();

        // The mail was queued with the content as it was; deleting the story afterwards does not break delivery.
        Assert.Equal(1, round.Sent);
    }

    // ---- O4: the publish and its announcement commit together (Security O3 S1, Reviewer O3 advisory 1) ----

    // Wraps the real announcer and can be told to fail, either before it queues anything or after it has saved the
    // queue rows, so the tests can look at what is left in the database when the announcement step breaks.
    private sealed class FaultyAnnouncer(ContentAnnouncer inner, FaultSwitch fault) : IContentAnnouncer
    {
        public async Task AnnounceAsync(ContentAnnouncement announcement, CancellationToken cancellationToken = default)
        {
            if (fault.Mode == FaultMode.BeforeQueue)
            {
                throw new InvalidOperationException("The queue could not be written.");
            }

            await inner.AnnounceAsync(announcement, cancellationToken);
            if (fault.Mode == FaultMode.AfterQueueSaved)
            {
                throw new InvalidOperationException("Something failed after the queue rows were saved.");
            }
        }
    }

    public enum FaultMode
    {
        None,
        BeforeQueue,
        AfterQueueSaved
    }

    private sealed class FaultSwitch
    {
        public volatile FaultMode Mode;
    }

    private static Action<IServiceCollection> Faulty(FaultSwitch fault) => services =>
    {
        services.AddScoped<ContentAnnouncer>();
        services.RemoveAll<IContentAnnouncer>();
        services.AddScoped<IContentAnnouncer>(provider => new FaultyAnnouncer(provider.GetRequiredService<ContentAnnouncer>(), fault));
    };

    [Theory]
    [InlineData(FaultMode.BeforeQueue)]
    [InlineData(FaultMode.AfterQueueSaved)]
    public async Task AStoryWhoseAnnouncementFailsStaysADraftUnmarkedAndUnqueued_AndPublishingAgainWorks(FaultMode mode)
    {
        var fault = new FaultSwitch { Mode = mode };
        using var site = await Site.StartAsync(configureServices: Faulty(fault));
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (id, slug) = await site.CreateStoryAsync();

        var failed = await site.PublishStoryAsync(id);

        Assert.True((int)failed.Status >= 500, "The publish request fails so Gustavo knows to try again.");
        var post = Assert.Single(await site.ReadAsync<BlogPost>());
        Assert.False(post.IsPublished);
        Assert.Null(post.PublishedAtUtc);
        Assert.Null(post.AnnouncedAtUtc);
        Assert.Empty(await site.ReadAsync<Announcement>());
        Assert.Empty(await site.ReadAsync<AnnouncementDelivery>());
        using var anonymous = site.Factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/blog/{slug}")).StatusCode);

        fault.Mode = FaultMode.None;
        Assert.Equal(HttpStatusCode.OK, (await site.PublishStoryAsync(id)).Status);
        await site.Dispatcher.DeliverPendingAsync();

        Assert.NotNull(Assert.Single(await site.ReadAsync<BlogPost>()).AnnouncedAtUtc);
        Assert.Single(await site.ReadAsync<Announcement>());
        Assert.Equal(Ada, Assert.Single(site.Mail.Rendered).To.Email);
    }

    [Theory]
    [InlineData(FaultMode.BeforeQueue)]
    [InlineData(FaultMode.AfterQueueSaved)]
    public async Task AProjectWhoseAnnouncementFailsStaysADraftUnmarkedAndUnqueued_AndPublishingAgainWorks(FaultMode mode)
    {
        var fault = new FaultSwitch { Mode = mode };
        using var site = await Site.StartAsync(configureServices: Faulty(fault));
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (id, _) = await site.CreateProjectAsync();

        var failed = await site.PublishProjectAsync(id);

        Assert.True((int)failed.Status >= 500);
        var project = Assert.Single(await site.ReadAsync<Project>());
        Assert.False(project.IsPublished);
        Assert.Null(project.PublishedAtUtc);
        Assert.Null(project.AnnouncedAtUtc);
        Assert.Empty(await site.ReadAsync<Announcement>());
        Assert.Empty(await site.ReadAsync<AnnouncementDelivery>());

        fault.Mode = FaultMode.None;
        Assert.Equal(HttpStatusCode.OK, (await site.PublishProjectAsync(id)).Status);
        await site.Dispatcher.DeliverPendingAsync();

        Assert.NotNull(Assert.Single(await site.ReadAsync<Project>()).AnnouncedAtUtc);
        Assert.Equal(Ada, Assert.Single(site.Mail.Rendered).To.Email);
    }

    [Fact]
    public async Task AMailProviderThatFailsAfterThePublishNeverUndoesItNorTheMarker()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        site.Mail.ShouldFail = true;
        var (id, _) = await site.CreateStoryAsync();

        Assert.Equal(HttpStatusCode.OK, (await site.PublishStoryAsync(id)).Status);
        await site.Dispatcher.DeliverPendingAsync();

        var post = Assert.Single(await site.ReadAsync<BlogPost>());
        Assert.True(post.IsPublished);
        Assert.NotNull(post.AnnouncedAtUtc);
        var delivery = Assert.Single(await site.ReadAsync<AnnouncementDelivery>());
        Assert.Null(delivery.SentAtUtc);
        Assert.Equal(1, delivery.Attempts);
    }

    // ---- O4 (c): two simultaneous first publishes ----

    private sealed class TemporaryDatabase(string path) : IDisposable
    {
        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            foreach (var file in new[] { path, path + "-wal", path + "-shm", path + "-journal" })
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    // A leftover temp file is harmless.
                }
            }
        }
    }

    // Two requests that really overlap need two database connections, so these tests use a file database (the other
    // tests share one in-memory connection, which cannot hold two transactions at once).
    private static Action<IServiceCollection> OnAFileDatabase(string path) => services =>
    {
        services.RemoveAll<ApplicationDbContext>();
        services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={path}"));
    };

    [Fact]
    public async Task TwoSimultaneousFirstPublishesOfAStoryNeitherFailNorMailAnyoneTwice()
    {
        var file = Path.Combine(Path.GetTempPath(), $"goosewebsite-race-{Guid.NewGuid():N}.db");
        using var cleanup = new TemporaryDatabase(file);
        using var site = await Site.StartAsync(configureServices: OnAFileDatabase(file));
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (id, _) = await site.CreateStoryAsync();

        var results = await Task.WhenAll(site.PublishStoryAsync(id), site.PublishStoryAsync(id));
        await site.Dispatcher.DeliverPendingAsync();
        await site.Dispatcher.DeliverPendingAsync();

        Assert.All(results, result => Assert.True((int)result.Status < 500, $"No publish answers with a server error; got {(int)result.Status}."));
        Assert.Contains(results, result => result.Status == HttpStatusCode.OK);
        Assert.True(Assert.Single(await site.ReadAsync<BlogPost>()).IsPublished);
        Assert.Single(await site.ReadAsync<Announcement>());
        Assert.Single(site.Mail.Rendered);
    }

    [Fact]
    public async Task TwoSimultaneousFirstPublishesOfAProjectNeitherFailNorMailAnyoneTwice()
    {
        var file = Path.Combine(Path.GetTempPath(), $"goosewebsite-race-{Guid.NewGuid():N}.db");
        using var cleanup = new TemporaryDatabase(file);
        using var site = await Site.StartAsync(configureServices: OnAFileDatabase(file));
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (id, _) = await site.CreateProjectAsync();

        var results = await Task.WhenAll(site.PublishProjectAsync(id), site.PublishProjectAsync(id));
        await site.Dispatcher.DeliverPendingAsync();

        Assert.All(results, result => Assert.True((int)result.Status < 500, $"No publish answers with a server error; got {(int)result.Status}."));
        Assert.Contains(results, result => result.Status == HttpStatusCode.OK);
        Assert.Single(await site.ReadAsync<Announcement>());
        Assert.Single(site.Mail.Rendered);
    }

    // ---- O4 (a): shutdown is not delayed by an announcement in flight ----

    [Fact]
    public async Task TheCancellationTokenOfARoundReachesTheMailService()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (id, _) = await site.CreateStoryAsync();
        await site.PublishStoryAsync(id);
        using var source = new CancellationTokenSource();

        await site.Dispatcher.DeliverPendingAsync(source.Token);

        Assert.Equal(source.Token, site.Mail.LastRenderedToken);
    }

    [Fact]
    public async Task ARoundCancelledDuringASendEndsPromptly_AndTheRecipientIsNotCountedAsFailed()
    {
        using var site = await Site.StartAsync();
        await site.AddSubscriberAsync("Ada Reader", Ada);
        var (id, _) = await site.CreateStoryAsync();
        await site.PublishStoryAsync(id);
        using var source = new CancellationTokenSource();
        site.Mail.OnRenderedSend = async token =>
        {
            source.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(30), token);
        };

        var started = DateTime.UtcNow;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => site.Dispatcher.DeliverPendingAsync(source.Token));

        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(10));
        var delivery = Assert.Single(await site.ReadAsync<AnnouncementDelivery>());
        Assert.Null(delivery.SentAtUtc);
        Assert.Equal(0, delivery.Attempts);
        site.Mail.OnRenderedSend = null;
        Assert.Equal(1, (await site.Dispatcher.DeliverPendingAsync()).Sent);
    }

    // ---- O4 (b): the summary rules cannot be made slow ----

    [Theory]
    [InlineData('[')]
    [InlineData('!')]
    public async Task AVeryLongRunOfBracketsInASummaryIsHandledQuickly(char opener)
    {
        using var site = await Site.StartAsync();
        using var scope = site.Factory.Services.CreateScope();
        var announcer = scope.ServiceProvider.GetRequiredService<IContentAnnouncer>();
        var hostile = new string(opener, 200_000) + "[x](" + new string('(', 1000);

        var started = DateTime.UtcNow;
        await announcer.AnnounceAsync(new ContentAnnouncement(AnnouncedContentKind.Story, 99, "Hostile", hostile, "/stories/hostile"));

        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(3), "The summary cleaning is bounded in time.");
        var stored = Assert.Single(await site.ReadAsync<Announcement>());
        Assert.True(stored.Summary.Length <= 300 + 1);
    }

    // ---- Dependency rule (architecture 3.1) ----

    [Fact]
    public void BlogAndProjectsDoNotReferenceTheSubscriptionsModule_TheyOnlyCallTheSharedAnnouncer()
    {
        var root = FindRepositoryRoot();
        foreach (var module in new[] { "Blog", "Projects" })
        {
            var files = Directory.EnumerateFiles(Path.Combine(root, "src", "GooseWebsite.Api", "Modules", module), "*.cs", SearchOption.AllDirectories);
            Assert.All(files, file => Assert.DoesNotContain("Modules.Subscriptions", File.ReadAllText(file)));
        }

        Assert.Contains("IContentAnnouncer", File.ReadAllText(Path.Combine(root, "src", "GooseWebsite.Api", "Modules", "Blog", "Services", "BlogAuthoringService.cs")));
        Assert.Contains("IContentAnnouncer", File.ReadAllText(Path.Combine(root, "src", "GooseWebsite.Api", "Modules", "Projects", "Services", "ProjectAuthoringService.cs")));
    }

    [Fact]
    public void WithoutTheSubscriptionsModuleThePublishStillWorks_BecauseSharedProvidesAQuietAnnouncer()
    {
        var services = new ServiceCollection();
        GooseWebsite.Api.Modules.Blog.BlogModule.AddBlogModule(services);

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<GooseWebsite.Api.Shared.Announcements.IContentAnnouncer>());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "GooseWebsite.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("GooseWebsite.slnx was not found above the test output.");
    }
}
