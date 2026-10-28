using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F5 follow-up (O6): what makes a post interactive survives the whole path. A GIF can be a cover and sit in the body,
// the summary and overview come back on the public API, emojis (several code points long) are stored untouched, and the
// public story list is not capped at 50.
public sealed class PostStructureTests
{
    private const string AdminEmail = "structure-admin@example.test";
    private const string Password = "Strong-password-123";

    private static readonly byte[] Gif = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 1, 0, 1, 0];

    // Family (ZWJ sequence), flag (regional indicators), thumbs-up with skin tone (modifier), keycap, red heart with a variation selector.
    private const string Emojis = "👨‍👩‍👧 🇧🇷 👍🏽 1️⃣ ❤️";

    private sealed class Editor(AuthApiFactory factory, HttpClient client)
    {
        public AuthApiFactory Factory { get; } = factory;

        public async Task<string> CsrfAsync() =>
            (await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken;

        public async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpMethod method, string path, object? body = null, string? csrf = null)
        {
            var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            request.Headers.Add("X-CSRF-TOKEN", csrf ?? await CsrfAsync());
            var response = await client.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            return (response.StatusCode, text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone());
        }

        public async Task<string> UploadGifAsync()
        {
            var form = new MultipartFormDataContent { { new ByteArrayContent(Gif), "file", "wave.gif" } };
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/editor/images") { Content = form };
            request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync());
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("url").GetString()!;
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

    [Fact]
    public async Task AGifCoverAndAnInlineGifSurviveOnAPublishedStory_WithTheirAltText()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var gif = await editor.UploadGifAsync();
        var body = $"Before.\n\n![A waving hand]({gif})\n\nAfter.";

        var created = await editor.SendAsync(HttpMethod.Post, "/api/editor/stories", new { title = "Animated", excerpt = "A short summary.", content = body, category = "Notes", tags = new[] { "life" }, coverImage = gif, coverImageAlt = "A looping wave" });
        var id = created.Body.GetProperty("id").GetInt32();
        var published = await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{id}/publish");

        Assert.Equal(HttpStatusCode.OK, published.Status);
        using var anonymous = editor.Factory.CreateHttpsClient();
        var story = await anonymous.GetFromJsonAsync<JsonElement>("/api/blog/animated");
        Assert.Equal(gif, story.GetProperty("coverImage").GetString());
        Assert.Equal("A looping wave", story.GetProperty("coverImageAlt").GetString());
        Assert.Equal(body, story.GetProperty("content").GetString());
        Assert.Equal("A short summary.", story.GetProperty("excerpt").GetString());
        var served = await anonymous.GetAsync(gif);
        Assert.Equal("image/gif", served.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Gif, await served.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task AGifCoverAndAnInlineGifSurviveOnAPublishedProject_WithTheirAltText()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var gif = await editor.UploadGifAsync();
        var overview = $"What it is.\n\n![A demo loop]({gif})";

        var created = await editor.SendAsync(HttpMethod.Post, "/api/editor/projects", new { title = "Animated project", overview, contribution = "All of it.", technologies = new[] { "C#" }, coverImage = gif, coverImageAlt = "A looping demo" });
        var published = await editor.SendAsync(HttpMethod.Post, $"/api/editor/projects/{created.Body.GetProperty("id").GetInt32()}/publish");

        Assert.Equal(HttpStatusCode.OK, published.Status);
        using var anonymous = editor.Factory.CreateHttpsClient();
        var project = await anonymous.GetFromJsonAsync<JsonElement>("/api/projects/animated-project");
        Assert.Equal(gif, project.GetProperty("coverImage").GetString());
        Assert.Equal("A looping demo", project.GetProperty("coverImageAlt").GetString());
        Assert.Equal(overview, project.GetProperty("overview").GetString());
    }

    [Fact]
    public async Task EmojisSurviveCreateEditAndThePublicApiOfAStory()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        string Text(string label) => $"{Emojis} {label} {Emojis}";
        var gif = await editor.UploadGifAsync();

        var created = await editor.SendAsync(HttpMethod.Post, "/api/editor/stories", new { title = Text("title"), excerpt = Text("excerpt"), content = Text("body"), category = Text("category"), tags = new[] { Emojis }, coverImage = gif, coverImageAlt = Text("alt") });
        Assert.Equal(HttpStatusCode.Created, created.Status);
        var id = created.Body.GetProperty("id").GetInt32();
        var edited = await editor.SendAsync(HttpMethod.Put, $"/api/editor/stories/{id}", new { title = Text("title 2"), excerpt = Text("excerpt 2"), content = Text("body 2"), category = Text("category"), tags = new[] { Emojis, "👍🏽" }, coverImage = gif, coverImageAlt = Text("alt") });
        Assert.Equal(HttpStatusCode.OK, edited.Status);
        var published = await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{id}/publish");

        using var anonymous = editor.Factory.CreateHttpsClient();
        var slug = published.Body.GetProperty("slug").GetString();
        var story = await anonymous.GetFromJsonAsync<JsonElement>($"/api/blog/{slug}");
        Assert.Equal(Text("title 2"), story.GetProperty("title").GetString());
        Assert.Equal(Text("excerpt 2"), story.GetProperty("excerpt").GetString());
        Assert.Equal(Text("body 2"), story.GetProperty("content").GetString());
        Assert.Equal(Text("category"), story.GetProperty("category").GetString());
        Assert.Equal(Text("alt"), story.GetProperty("coverImageAlt").GetString());
        Assert.Equal([Emojis, "👍🏽"], story.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()).ToArray());
        var listed = (await anonymous.GetFromJsonAsync<JsonElement>("/api/blog")).EnumerateArray().Single();
        Assert.Equal(Text("title 2"), listed.GetProperty("title").GetString());
        Assert.Equal(Text("excerpt 2"), listed.GetProperty("excerpt").GetString());
    }

    [Fact]
    public async Task EmojisSurviveCreateEditAndThePublicApiOfAProject()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        string Text(string label) => $"{Emojis} {label} {Emojis}";
        var gif = await editor.UploadGifAsync();

        var created = await editor.SendAsync(HttpMethod.Post, "/api/editor/projects", new { title = Text("title"), overview = Text("overview"), contribution = Text("contribution"), technologies = new[] { Emojis }, coverImage = gif, coverImageAlt = Text("alt") });
        Assert.Equal(HttpStatusCode.Created, created.Status);
        var id = created.Body.GetProperty("id").GetInt32();
        var edited = await editor.SendAsync(HttpMethod.Put, $"/api/editor/projects/{id}", new { title = Text("title 2"), overview = Text("overview 2"), contribution = Text("contribution 2"), technologies = new[] { Emojis, "🇧🇷" }, coverImage = gif, coverImageAlt = Text("alt") });
        Assert.Equal(HttpStatusCode.OK, edited.Status);
        var published = await editor.SendAsync(HttpMethod.Post, $"/api/editor/projects/{id}/publish");

        using var anonymous = editor.Factory.CreateHttpsClient();
        var project = await anonymous.GetFromJsonAsync<JsonElement>($"/api/projects/{published.Body.GetProperty("slug").GetString()}");
        Assert.Equal(Text("title 2"), project.GetProperty("title").GetString());
        Assert.Equal(Text("overview 2"), project.GetProperty("overview").GetString());
        Assert.Equal(Text("contribution 2"), project.GetProperty("contribution").GetString());
        Assert.Equal(Text("alt"), project.GetProperty("coverImageAlt").GetString());
        Assert.Equal([Emojis, "🇧🇷"], project.GetProperty("technologies").EnumerateArray().Select(item => item.GetString()).ToArray());
    }

    [Fact]
    public async Task ThePublicStoryListIsNotCappedAtFifty()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var csrf = await editor.CsrfAsync();
        for (var number = 1; number <= 55; number++)
        {
            var created = await editor.SendAsync(HttpMethod.Post, "/api/editor/stories", new { title = $"Story {number}", excerpt = "e", content = "c", category = "Notes", tags = Array.Empty<string>() }, csrf);
            Assert.Equal(HttpStatusCode.Created, created.Status);
            var published = await editor.SendAsync(HttpMethod.Post, $"/api/editor/stories/{created.Body.GetProperty("id").GetInt32()}/publish", null, csrf);
            Assert.Equal(HttpStatusCode.OK, published.Status);
        }

        using var anonymous = editor.Factory.CreateHttpsClient();
        async Task<int> CountAsync(string query) => (await anonymous.GetFromJsonAsync<JsonElement>($"/api/blog{query}")).GetArrayLength();

        Assert.Equal(55, await CountAsync("?pageSize=2147483647"));
        Assert.Equal(55, await CountAsync("?pageSize=100"));
        Assert.Equal(12, await CountAsync(string.Empty));
        Assert.Equal(1, await CountAsync("?pageSize=0"));
        Assert.Equal(1, await CountAsync("?pageSize=-5"));
        Assert.Equal(5, await CountAsync("?pageSize=50&page=2"));
        Assert.Equal(0, await CountAsync("?pageSize=2147483647&page=2147483647"));
    }
}
