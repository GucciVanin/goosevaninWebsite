using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F4-US3: Gustavo creates and edits a story as a draft, previews it, and publishes it explicitly.
// Saving never publishes; publishing happens once; a draft is never readable by the public.
public sealed class StoryEditorApiTests
{
    private const string AdminEmail = "stories-admin@example.test";
    private const string Password = "Strong-password-123";

    private sealed class Editor(AuthApiFactory factory, HttpClient client)
    {
        public AuthApiFactory Factory { get; } = factory;

        public HttpClient Client { get; } = client;

        public async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpMethod method, string path, object? body = null)
        {
            var csrf = (await (await Client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken;
            var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            request.Headers.Add("X-CSRF-TOKEN", csrf);
            var response = await Client.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            var json = text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone();
            return (response.StatusCode, json);
        }

        public Task<(HttpStatusCode Status, JsonElement Body)> CreateAsync(object body) => SendAsync(HttpMethod.Post, "/api/editor/stories", body);

        public async Task<HttpStatusCode> PublicStatusAsync(string slug)
        {
            using var anonymous = Factory.CreateHttpsClient();
            return (await anonymous.GetAsync($"/api/blog/{slug}")).StatusCode;
        }
    }

    private static async Task<Editor> SignedInEditorAsync()
    {
        var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, Password);
        var client = factory.CreateHttpsClient();
        var csrf = (await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken;
        var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new { email = AdminEmail, password = Password }) };
        login.Headers.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(login)).StatusCode);
        return new Editor(factory, client);
    }

    private static object Story(string title = "A first story", string excerpt = "Why it matters.", string content = "# Heading\n\nSome *text*.", string[]? tags = null) =>
        new { title, excerpt, content, category = "Notes", tags = tags ?? ["life"] };

    [Fact]
    public async Task ANewStoryIsAlwaysADraft_AndTheFieldsComeBackAsSaved()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;

        // Even a request that asks to publish (the old field) only saves a draft.
        var created = await editor.CreateAsync(new { title = "A first story", excerpt = "Why it matters.", content = "# Heading", category = "Notes", tags = new[] { "life" }, isPublished = true });

        Assert.Equal(HttpStatusCode.Created, created.Status);
        var id = created.Body.GetProperty("id").GetInt32();
        var reopened = await editor.SendAsync(HttpMethod.Get, $"/api/editor/stories/{id}");
        Assert.Equal(HttpStatusCode.OK, reopened.Status);
        Assert.Equal("A first story", reopened.Body.GetProperty("title").GetString());
        Assert.Equal("Why it matters.", reopened.Body.GetProperty("excerpt").GetString());
        Assert.Equal("# Heading", reopened.Body.GetProperty("content").GetString());
        Assert.Equal("Notes", reopened.Body.GetProperty("category").GetString());
        Assert.Equal(["life"], reopened.Body.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()).ToArray());
        Assert.False(reopened.Body.GetProperty("isPublished").GetBoolean());
        Assert.Equal("a-first-story", reopened.Body.GetProperty("slug").GetString());
    }

    [Fact]
    public async Task ADraftCannotBeReadByThePublic_ButTheEditorCanPreviewIt()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var created = await editor.CreateAsync(Story());
        var slug = created.Body.GetProperty("slug").GetString()!;

        Assert.Equal(HttpStatusCode.NotFound, await editor.PublicStatusAsync(slug));
        using var anonymous = editor.Factory.CreateHttpsClient();
        Assert.DoesNotContain(slug, await anonymous.GetStringAsync("/api/blog"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/editor/stories/{created.Body.GetProperty("id").GetInt32()}")).StatusCode);
        var preview = await editor.SendAsync(HttpMethod.Get, $"/api/editor/stories/{created.Body.GetProperty("id").GetInt32()}");
        Assert.Equal(HttpStatusCode.OK, preview.Status);
    }

    [Fact]
    public async Task SavingEditsDoesNotPublish_AndTheSlugStaysTheSameWhenTheTitleChanges()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var created = await editor.CreateAsync(Story());
        var id = created.Body.GetProperty("id").GetInt32();

        var saved = await editor.SendAsync(HttpMethod.Put, $"/api/editor/stories/{id}", new { title = "A new title", excerpt = "Changed.", content = "Body", category = "Notes", tags = new[] { "life" }, isPublished = true });

        Assert.Equal(HttpStatusCode.OK, saved.Status);
        Assert.False(saved.Body.GetProperty("isPublished").GetBoolean());
        Assert.Equal("a-first-story", saved.Body.GetProperty("slug").GetString());
        Assert.Equal("A new title", saved.Body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PublishMakesTheStoryPublicOnce_AndPublishingAgainChangesNothing()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var created = await editor.CreateAsync(Story());
        var id = created.Body.GetProperty("id").GetInt32();
        var slug = created.Body.GetProperty("slug").GetString()!;

        var published = await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{id}/publish");
        var publishedAt = published.Body.GetProperty("publishedAtUtc").GetDateTime();
        var again = await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{id}/publish");

        Assert.Equal(HttpStatusCode.OK, published.Status);
        Assert.True(published.Body.GetProperty("isPublished").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, await editor.PublicStatusAsync(slug));
        Assert.Equal(publishedAt, again.Body.GetProperty("publishedAtUtc").GetDateTime());
        Assert.Equal(published.Body.GetProperty("updatedAtUtc").GetDateTime(), again.Body.GetProperty("updatedAtUtc").GetDateTime());
    }

    [Fact]
    public async Task APublishedStoryCanBeReturnedToDraft_AndKeepsItsPublicationDate()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var created = await editor.CreateAsync(Story());
        var id = created.Body.GetProperty("id").GetInt32();
        var slug = created.Body.GetProperty("slug").GetString()!;
        var published = await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{id}/publish");

        var back = await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{id}/unpublish");

        Assert.Equal(HttpStatusCode.OK, back.Status);
        Assert.False(back.Body.GetProperty("isPublished").GetBoolean());
        Assert.Equal(published.Body.GetProperty("publishedAtUtc").GetDateTime(), back.Body.GetProperty("publishedAtUtc").GetDateTime());
        Assert.Equal(HttpStatusCode.NotFound, await editor.PublicStatusAsync(slug));
    }

    [Fact]
    public async Task ThePublicationDateCanBeSetByGustavo()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var created = await editor.CreateAsync(Story());
        var id = created.Body.GetProperty("id").GetInt32();
        await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{id}/publish");

        var older = new DateTime(2024, 5, 17, 9, 30, 0, DateTimeKind.Utc);
        var saved = await editor.SendAsync(HttpMethod.Put, $"/api/editor/stories/{id}", new { title = "A first story", excerpt = "e", content = "c", category = "Notes", tags = new[] { "life" }, publishedAtUtc = older });

        Assert.Equal(HttpStatusCode.OK, saved.Status);
        Assert.Equal(older, saved.Body.GetProperty("publishedAtUtc").GetDateTime());
        Assert.True(saved.Body.GetProperty("isPublished").GetBoolean());
    }

    [Theory]
    [InlineData("", "e", "c", "title")]
    [InlineData("   ", "e", "c", "title")]
    public async Task ASaveWithoutATitleIsRefusedAndNamesTheField(string title, string excerpt, string content, string field)
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;

        var refused = await editor.CreateAsync(new { title, excerpt, content, category = "Notes", tags = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.BadRequest, refused.Status);
        Assert.Contains(field, refused.Body.GetProperty("errors").EnumerateObject().Select(error => error.Name.ToLowerInvariant()));
    }

    [Fact]
    public async Task ADraftMayBeSavedIncomplete_ButCannotBePublishedUntilItIsComplete()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var created = await editor.CreateAsync(new { title = "Only a title", excerpt = "", content = "", category = "Notes", tags = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.Created, created.Status);
        var id = created.Body.GetProperty("id").GetInt32();
        var slug = created.Body.GetProperty("slug").GetString()!;

        var refused = await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{id}/publish");

        Assert.Equal(HttpStatusCode.BadRequest, refused.Status);
        var fields = refused.Body.GetProperty("errors").EnumerateObject().Select(error => error.Name.ToLowerInvariant()).ToList();
        Assert.Contains("excerpt", fields);
        Assert.Contains("content", fields);
        Assert.Equal(HttpStatusCode.NotFound, await editor.PublicStatusAsync(slug));
        Assert.False((await editor.SendAsync(HttpMethod.Get, $"/api/editor/stories/{id}")).Body.GetProperty("isPublished").GetBoolean());
    }

    [Fact]
    public async Task TagsAreCleanedAndATagWithACommaIsRefused()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;

        var cleaned = await editor.CreateAsync(Story(tags: ["  Life ", "life", "", "Work", "WORK"]));
        var refused = await editor.CreateAsync(Story(title: "Second", tags: ["one,two"]));

        Assert.Equal(["Life", "Work"], cleaned.Body.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()).ToArray());
        Assert.Equal(HttpStatusCode.BadRequest, refused.Status);
        Assert.Contains("tags", refused.Body.GetProperty("errors").EnumerateObject().Select(error => error.Name.ToLowerInvariant()));
    }

    [Fact]
    public async Task TwoStoriesWithTheSameTitleGetDifferentAddresses()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;

        var first = await editor.CreateAsync(Story(title: "Same Title!"));
        var second = await editor.CreateAsync(Story(title: "Same Title!"));

        Assert.Equal("same-title", first.Body.GetProperty("slug").GetString());
        Assert.Equal("same-title-2", second.Body.GetProperty("slug").GetString());
    }

    [Theory]
    [InlineData("Café & Crème — 2026!", "cafe-creme-2026")]
    [InlineData("  Leading   and trailing  ", "leading-and-trailing")]
    [InlineData("日本語", "story")]
    [InlineData("???", "story")]
    public async Task TheAddressIsMadeFromTheTitleUsingLettersAndDigitsOnly(string title, string expectedSlug)
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;

        var created = await editor.CreateAsync(Story(title: title));

        Assert.Equal(HttpStatusCode.Created, created.Status);
        Assert.Equal(expectedSlug, created.Body.GetProperty("slug").GetString());
    }

    [Fact]
    public async Task TheWorkspaceListShowsDraftsAndPublishedStoriesWithTheirStatusAndDates()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        await editor.CreateAsync(Story(title: "Draft one"));
        var live = await editor.CreateAsync(Story(title: "Live one"));
        await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{live.Body.GetProperty("id").GetInt32()}/publish");

        var list = await editor.SendAsync(HttpMethod.Get, "/api/editor/stories");

        Assert.Equal(HttpStatusCode.OK, list.Status);
        var rows = list.Body.EnumerateArray().ToDictionary(row => row.GetProperty("title").GetString()!);
        Assert.False(rows["Draft one"].GetProperty("isPublished").GetBoolean());
        Assert.True(rows["Live one"].GetProperty("isPublished").GetBoolean());
        Assert.True(rows["Live one"].TryGetProperty("updatedAtUtc", out _));
        Assert.False(rows["Draft one"].TryGetProperty("content", out _));
    }

    [Fact]
    public async Task DeletingAStoryRemovesItForGood()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var created = await editor.CreateAsync(Story());
        var id = created.Body.GetProperty("id").GetInt32();
        var slug = created.Body.GetProperty("slug").GetString()!;
        await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{id}/publish");

        var deleted = await editor.SendAsync(HttpMethod.Delete, $"/api/editor/stories/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.Status);
        Assert.Equal(HttpStatusCode.NotFound, await editor.PublicStatusAsync(slug));
        Assert.Equal(HttpStatusCode.NotFound, (await editor.SendAsync(HttpMethod.Get, $"/api/editor/stories/{id}")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await editor.SendAsync(HttpMethod.Delete, $"/api/editor/stories/{id}")).Status);
    }

    [Fact]
    public async Task APublishedStoryIsListedPublicallyNewestPublicationFirst()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var older = await editor.CreateAsync(Story(title: "Older"));
        var newer = await editor.CreateAsync(Story(title: "Newer"));
        await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{newer.Body.GetProperty("id").GetInt32()}/publish");
        await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{older.Body.GetProperty("id").GetInt32()}/publish");
        await editor.SendAsync(HttpMethod.Put, $"/api/editor/stories/{older.Body.GetProperty("id").GetInt32()}", new { title = "Older", excerpt = "e", content = "c", category = "Notes", tags = Array.Empty<string>(), publishedAtUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

        using var anonymous = editor.Factory.CreateHttpsClient();
        var titles = (await anonymous.GetFromJsonAsync<JsonElement>("/api/blog")).EnumerateArray().Select(row => row.GetProperty("title").GetString()).ToArray();

        Assert.Equal(["Newer", "Older"], titles);
    }
}
