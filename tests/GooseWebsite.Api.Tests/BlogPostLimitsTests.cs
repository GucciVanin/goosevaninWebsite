using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Gustavo is the only author, so a story's text has no length limit (decided with F7-US1, 2026-10-14).
// The API request used to cap the title, excerpt and category; that contradicted the decision.
public sealed class BlogPostLimitsTests
{
    private const string AdminEmail = "limits-admin@example.test";
    private const string Password = "Strong-password-123";

    [Fact]
    public async Task AnAdministratorCanSaveAStoryWithAVeryLongTitleExcerptCategoryAndBody()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, Password);
        using var client = factory.CreateHttpsClient();
        var csrf = (await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken;
        var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new { email = AdminEmail, password = Password }) };
        login.Headers.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(login)).StatusCode);

        var create = new HttpRequestMessage(HttpMethod.Post, "/api/editor/stories")
        {
            Content = JsonContent.Create(new
            {
                title = new string('t', 1000),
                excerpt = new string('e', 5000),
                content = new string('c', 200_000),
                category = new string('k', 500),
                tags = new[] { "long" }
            })
        };
        create.Headers.Add("X-CSRF-TOKEN", (await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken);

        var response = await client.SendAsync(create);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
