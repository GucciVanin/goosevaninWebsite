using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace GooseWebsite.Api.Tests;

// R1-G2 (work item W1): the public story and project responses carry only what the public pages show.
// No account id, no database id, no editor state. The editor responses keep what the editor needs.
public sealed class PublicContractTests
{
    private const string AdminEmail = "contract-admin@example.test";
    private const string Password = "Strong-password-123";

    private static readonly string[] StoryKeys =
        ["category", "content", "coverImage", "coverImageAlt", "createdAtUtc", "excerpt", "publishedAtUtc", "slug", "tags", "title"];

    private static readonly string[] ProjectKeys =
        ["contribution", "coverImage", "coverImageAlt", "demoUrl", "overview", "publishedAtUtc", "slug", "sourceUrl", "technologies", "title"];

    private static async Task<(AuthApiFactory Factory, HttpClient Client)> SignedInAsync()
    {
        var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, Password);
        var client = factory.CreateHttpsClient();
        var csrf = (await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken;
        var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new { email = AdminEmail, password = Password }) };
        login.Headers.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(login)).StatusCode);
        return (factory, client);
    }

    private static async Task<JsonElement> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Add("X-CSRF-TOKEN", (await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken);
        var response = await client.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode, $"{method} {path} answered {(int)response.StatusCode}");
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private static string[] Keys(JsonElement element) =>
        element.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();

    [Fact]
    public async Task PublicStoryResponses_CarryNoAccountIdNoDatabaseIdAndNoEditorState()
    {
        var (factory, client) = await SignedInAsync();
        using var host = factory;
        var created = await SendAsync(client, HttpMethod.Post, "/api/editor/stories", new { title = "Public shape", excerpt = "e", content = "c", category = "Notes", tags = new[] { "a" } });
        var editorKeys = Keys(created);
        var authorId = created.GetProperty("authorId").GetString()!;
        await SendAsync(client, HttpMethod.Post, $"/api/editor/stories/{created.GetProperty("id").GetInt32()}/publish");

        using var anonymous = factory.CreateHttpsClient();
        var detailText = await anonymous.GetStringAsync("/api/blog/public-shape");
        var listText = await anonymous.GetStringAsync("/api/blog");

        Assert.Equal(StoryKeys, Keys(JsonDocument.Parse(detailText).RootElement));
        Assert.Equal(StoryKeys, Keys(JsonDocument.Parse(listText).RootElement.EnumerateArray().Single()));
        Assert.DoesNotContain(authorId, detailText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(authorId, listText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authorId", detailText + listText, StringComparison.OrdinalIgnoreCase);
        // The editor keeps its own shape.
        Assert.Contains("authorId", editorKeys);
        Assert.Contains("id", editorKeys);
        Assert.Contains("isPublished", editorKeys);
    }

    [Fact]
    public async Task PublicProjectResponses_CarryNoDatabaseIdAndNoEditorState()
    {
        var (factory, client) = await SignedInAsync();
        using var host = factory;
        var created = await SendAsync(client, HttpMethod.Post, "/api/editor/projects", new { title = "Public project", overview = "o", contribution = "c", technologies = new[] { "C#" } });
        var editorKeys = Keys(created);
        await SendAsync(client, HttpMethod.Post, $"/api/editor/projects/{created.GetProperty("id").GetInt32()}/publish");

        using var anonymous = factory.CreateHttpsClient();
        var detail = JsonDocument.Parse(await anonymous.GetStringAsync("/api/projects/public-project")).RootElement;
        var listed = JsonDocument.Parse(await anonymous.GetStringAsync("/api/projects")).RootElement.EnumerateArray().Single();

        Assert.Equal(ProjectKeys, Keys(detail));
        Assert.Equal(ProjectKeys, Keys(listed));
        Assert.Contains("id", editorKeys);
        Assert.Contains("isPublished", editorKeys);
    }
}
