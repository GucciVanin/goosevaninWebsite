using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F4-US4: Gustavo creates and edits a project as a draft, previews it, and publishes it explicitly.
public sealed class ProjectEditorApiTests
{
    private const string AdminEmail = "projects-admin@example.test";
    private const string Password = "Strong-password-123";

    private sealed class Editor(AuthApiFactory factory, HttpClient client)
    {
        public AuthApiFactory Factory { get; } = factory;

        public async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpMethod method, string path, object? body = null)
        {
            var csrf = (await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken;
            var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            request.Headers.Add("X-CSRF-TOKEN", csrf);
            var response = await client.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            return (response.StatusCode, text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone());
        }

        public Task<(HttpStatusCode Status, JsonElement Body)> CreateAsync(object body) => SendAsync(HttpMethod.Post, "/api/editor/projects", body);

        public async Task<HttpStatusCode> PublicStatusAsync(string slug)
        {
            using var anonymous = Factory.CreateHttpsClient();
            return (await anonymous.GetAsync($"/api/projects/{slug}")).StatusCode;
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

    private static object Project(string title = "Goose website", string overview = "A personal site.", string? demoUrl = "https://example.test/demo", string? sourceUrl = "https://github.com/example/site") =>
        new { title, overview, contribution = "Everything.", technologies = new[] { "C#", "Angular" }, demoUrl, sourceUrl };

    [Fact]
    public async Task ANewProjectIsADraft_AndEveryFieldComesBackAsSaved()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;

        var created = await editor.CreateAsync(Project());

        Assert.Equal(HttpStatusCode.Created, created.Status);
        var reopened = await editor.SendAsync(HttpMethod.Get, $"/api/editor/projects/{created.Body.GetProperty("id").GetInt32()}");
        Assert.Equal("Goose website", reopened.Body.GetProperty("title").GetString());
        Assert.Equal("A personal site.", reopened.Body.GetProperty("overview").GetString());
        Assert.Equal("Everything.", reopened.Body.GetProperty("contribution").GetString());
        Assert.Equal(["C#", "Angular"], reopened.Body.GetProperty("technologies").EnumerateArray().Select(item => item.GetString()).ToArray());
        Assert.Equal("https://example.test/demo", reopened.Body.GetProperty("demoUrl").GetString());
        Assert.Equal("https://github.com/example/site", reopened.Body.GetProperty("sourceUrl").GetString());
        Assert.False(reopened.Body.GetProperty("isPublished").GetBoolean());
        Assert.Equal("goose-website", reopened.Body.GetProperty("slug").GetString());
    }

    [Fact]
    public async Task ADraftIsPrivate_ButTheEditorCanPreviewIt()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var created = await editor.CreateAsync(Project());
        var id = created.Body.GetProperty("id").GetInt32();
        var slug = created.Body.GetProperty("slug").GetString()!;

        Assert.Equal(HttpStatusCode.NotFound, await editor.PublicStatusAsync(slug));
        using var anonymous = editor.Factory.CreateHttpsClient();
        Assert.DoesNotContain(slug, await anonymous.GetStringAsync("/api/projects"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/editor/projects/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await editor.SendAsync(HttpMethod.Get, $"/api/editor/projects/{id}")).Status);
    }

    [Fact]
    public async Task SavingDoesNotPublish_AndPublishingMakesItPublicOnce()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var created = await editor.CreateAsync(Project());
        var id = created.Body.GetProperty("id").GetInt32();
        var slug = created.Body.GetProperty("slug").GetString()!;

        var saved = await editor.SendAsync(HttpMethod.Put, $"/api/editor/projects/{id}", new { title = "Renamed", overview = "Changed.", isPublished = true });
        Assert.False(saved.Body.GetProperty("isPublished").GetBoolean());
        Assert.Equal(slug, saved.Body.GetProperty("slug").GetString());
        Assert.Equal(HttpStatusCode.NotFound, await editor.PublicStatusAsync(slug));

        var published = await editor.SendAsync(HttpMethod.Post, $"/api/editor/projects/{id}/publish");
        Assert.Equal(HttpStatusCode.OK, published.Status);
        var firstDate = published.Body.GetProperty("publishedAtUtc").GetString();
        Assert.Equal(HttpStatusCode.OK, await editor.PublicStatusAsync(slug));

        var again = await editor.SendAsync(HttpMethod.Post, $"/api/editor/projects/{id}/publish");
        Assert.Equal(firstDate, again.Body.GetProperty("publishedAtUtc").GetString());

        var back = await editor.SendAsync(HttpMethod.Post, $"/api/editor/projects/{id}/unpublish");
        Assert.False(back.Body.GetProperty("isPublished").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, await editor.PublicStatusAsync(slug));
    }

    [Fact]
    public async Task AProjectWithoutAnOverviewCannotBePublished()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;
        var created = await editor.CreateAsync(new { title = "Bare" });
        var id = created.Body.GetProperty("id").GetInt32();

        var published = await editor.SendAsync(HttpMethod.Post, $"/api/editor/projects/{id}/publish");

        Assert.Equal(HttpStatusCode.BadRequest, published.Status);
        Assert.True(published.Body.GetProperty("errors").TryGetProperty("overview", out _));
        Assert.False((await editor.SendAsync(HttpMethod.Get, $"/api/editor/projects/{id}")).Body.GetProperty("isPublished").GetBoolean());
    }

    [Theory]
    [InlineData("javascript:alert(1)", "demoUrl")]
    [InlineData("ftp://example.test/file", "demoUrl")]
    [InlineData("not a url", "sourceUrl")]
    [InlineData("//example.test/x", "sourceUrl")]
    public async Task ALinkThatIsNotAWebAddressIsRefusedWithTheFieldNamed(string link, string field)
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;

        var result = field == "demoUrl"
            ? await editor.CreateAsync(Project(demoUrl: link))
            : await editor.CreateAsync(Project(sourceUrl: link));

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.True(result.Body.GetProperty("errors").TryGetProperty(field, out _));
        var list = await editor.SendAsync(HttpMethod.Get, "/api/editor/projects");
        Assert.Equal(0, list.Body.GetArrayLength());
    }

    [Fact]
    public async Task ATitleIsRequired_AndTechnologiesAreCleaned()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;

        var missing = await editor.CreateAsync(new { title = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, missing.Status);

        var commas = await editor.CreateAsync(new { title = "X", technologies = new[] { "a,b" } });
        Assert.True(commas.Body.GetProperty("errors").TryGetProperty("technologies", out _));

        var cleaned = await editor.CreateAsync(new { title = "X", technologies = new[] { " C# ", "c#", "", "Angular" } });
        Assert.Equal(["C#", "Angular"], cleaned.Body.GetProperty("technologies").EnumerateArray().Select(item => item.GetString()).ToArray());
    }

    [Fact]
    public async Task ProjectsWithTheSameTitleGetDifferentAddresses_AndCanBeDeleted()
    {
        var editor = await SignedInEditorAsync();
        using var host = editor.Factory;

        var first = await editor.CreateAsync(Project());
        var second = await editor.CreateAsync(Project());

        Assert.Equal("goose-website", first.Body.GetProperty("slug").GetString());
        Assert.Equal("goose-website-2", second.Body.GetProperty("slug").GetString());
        var id = second.Body.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await editor.SendAsync(HttpMethod.Delete, $"/api/editor/projects/{id}")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await editor.SendAsync(HttpMethod.Get, $"/api/editor/projects/{id}")).Status);
    }
}
