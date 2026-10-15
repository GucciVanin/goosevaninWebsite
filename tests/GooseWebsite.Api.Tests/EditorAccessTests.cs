using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using GooseWebsite.Api.Modules.Blog.Domain;
using GooseWebsite.Api.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F4-US2: only a provisioned administrator with a live session can change content. Every write endpoint is checked
// for an anonymous caller, a Reader, an expired session and an administrator, and the stored content is asserted.
public sealed class EditorAccessTests
{
    private const string AdminEmail = "editor-admin@example.test";
    private const string ReaderEmail = "editor-reader@example.test";
    private const string Password = "Strong-password-123";

    private static object PostBody(string title) => new
    {
        title,
        excerpt = "An excerpt.",
        content = "Some content.",
        category = "Testing",
        tags = new[] { "editor" },
        isPublished = true
    };

    private static async Task<(int Id, string Slug)> SeedPostAsync(AuthApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        var author = await database.FindUserByEmailAsync(AdminEmail);
        var post = new BlogPost { Title = "Original", Slug = "original", Excerpt = "e", Content = "c", IsPublished = true, AuthorId = author!.Id };
        database.Add(post);
        await database.SaveChangesAsync();
        return (post.Id, post.Slug);
    }

    private static async Task<(string Title, bool Exists)> StoredAsync(AuthApiFactory factory, int id)
    {
        using var scope = factory.Services.CreateScope();
        var post = await scope.ServiceProvider.GetRequiredService<DatabaseService>().Read<BlogPost>().SingleOrDefaultAsync(p => p.Id == id);
        return (post?.Title ?? string.Empty, post is not null);
    }

    private static async Task SignInAsync(HttpClient client, string email)
    {
        var csrf = await GetCsrfAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new { email, password = Password }) };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode);
    }

    private static async Task<string> GetCsrfAsync(HttpClient client) =>
        (await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken;

    private static async Task<HttpStatusCode> WriteAsync(HttpClient client, string verb, int id)
    {
        var csrf = await GetCsrfAsync(client);
        var (method, path, body) = verb switch
        {
            "create" => (HttpMethod.Post, "/api/blog", PostBody("Created")),
            "update" => (HttpMethod.Put, $"/api/blog/{id}", PostBody("Changed")),
            _ => (HttpMethod.Delete, $"/api/blog/{id}", (object?)null)
        };
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return (await client.SendAsync(request)).StatusCode;
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Anonymous_CannotWrite_AndTheStoredContentIsUnchanged(string verb)
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, Password);
        var (id, _) = await SeedPostAsync(factory);
        using var client = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.Unauthorized, await WriteAsync(client, verb, id));

        Assert.Equal((Title: "Original", Exists: true), await StoredAsync(factory, id));
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task AReader_CannotWrite_AndTheStoredContentIsUnchanged(string verb)
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, Password);
        await factory.ProvisionStandardAccountAsync(ReaderEmail, Password);
        var (id, _) = await SeedPostAsync(factory);
        using var client = factory.CreateHttpsClient();
        await SignInAsync(client, ReaderEmail);

        Assert.Equal(HttpStatusCode.Forbidden, await WriteAsync(client, verb, id));

        Assert.Equal((Title: "Original", Exists: true), await StoredAsync(factory, id));
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task AnExpiredSession_CannotWrite_AndTheStoredContentIsUnchanged(string verb)
    {
        using var factory = new AuthApiFactory(cookieLifetime: TimeSpan.FromMilliseconds(400));
        await factory.ProvisionAdministratorAsync(AdminEmail, Password);
        var (id, _) = await SeedPostAsync(factory);
        using var client = factory.CreateHttpsClient();
        await SignInAsync(client, AdminEmail);
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        Assert.Equal(HttpStatusCode.Unauthorized, await WriteAsync(client, verb, id));

        Assert.Equal((Title: "Original", Exists: true), await StoredAsync(factory, id));
    }

    [Fact]
    public async Task EveryProvisionedAdministrator_CanCreateUpdateAndDelete()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, Password);
        await factory.ProvisionAdministratorAsync("second-editor@example.test", Password);
        var (id, _) = await SeedPostAsync(factory);
        using var client = factory.CreateHttpsClient();
        await SignInAsync(client, "second-editor@example.test");

        Assert.Equal(HttpStatusCode.Created, await WriteAsync(client, "create", id));
        Assert.Equal(HttpStatusCode.OK, await WriteAsync(client, "update", id));
        Assert.Equal("Changed", (await StoredAsync(factory, id)).Title);
        Assert.Equal(HttpStatusCode.NoContent, await WriteAsync(client, "delete", id));
        Assert.False((await StoredAsync(factory, id)).Exists);
    }

    [Fact]
    public async Task AnAdministratorWithoutTheCsrfToken_CannotWrite()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, Password);
        var (id, _) = await SeedPostAsync(factory);
        using var client = factory.CreateHttpsClient();
        await SignInAsync(client, AdminEmail);

        var response = await client.PutAsJsonAsync($"/api/blog/{id}", PostBody("No token"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Original", (await StoredAsync(factory, id)).Title);
    }
}
