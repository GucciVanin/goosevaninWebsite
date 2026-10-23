using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F9-US7 (signed-out probe): the browser app asks GET /api/auth/me only when it has a sign-in marker, so a signed-out
// visitor never triggers a 401. The marker is a plain (not HttpOnly) cookie that carries no identity and no token; it
// is only a hint. The HttpOnly session cookie stays the only proof of a session, and the API still checks it.
public sealed class SessionMarkerCookieTests
{
    private const string Email = "marker-admin@example.test";
    private const string Password = "Strong-password-123";
    private const string MarkerName = "goosewebsite.signedin";

    [Fact]
    public async Task SignIn_SetsAMarkerCookieWithTheAgreedFlagsAndNoIdentity()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(Email, Password);
        using var browser = factory.CreateHttpsClient();

        var login = await SignInAsync(browser, Email, Password);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var marker = MarkerHeader(login);
        Assert.NotNull(marker);
        var lower = marker!.ToLowerInvariant();
        Assert.Contains("secure", lower);
        Assert.Contains("samesite=lax", lower);
        Assert.Contains("path=/", lower);
        Assert.DoesNotContain("httponly", lower);
        // A session cookie: it goes away with the browser session, like the session cookie it mirrors.
        Assert.DoesNotContain("expires=", lower);
        Assert.DoesNotContain("max-age=", lower);
        // No identity and no token in the value.
        var value = marker.Split(';')[0][(MarkerName.Length + 1)..];
        Assert.Equal("1", value);
        Assert.DoesNotContain("marker-admin", marker, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Site administrator", marker);
    }

    [Fact]
    public async Task SignIn_KeepsTheSessionCookieHttpOnly()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(Email, Password);
        using var browser = factory.CreateHttpsClient();

        var login = await SignInAsync(browser, Email, Password);

        var session = login.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("goosewebsite.auth=", StringComparison.Ordinal));
        Assert.Contains("httponly", session.ToLowerInvariant());
        Assert.Contains("secure", session.ToLowerInvariant());
    }

    [Theory]
    [InlineData("marker-admin@example.test", "Wrong-password-123")]
    [InlineData("nobody@example.test", "Wrong-password-123")]
    public async Task FailedSignIn_DoesNotSetTheMarker(string email, string password)
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(Email, Password);
        using var browser = factory.CreateHttpsClient();

        var login = await SignInAsync(browser, email, password);

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Null(MarkerHeader(login));
    }

    [Fact]
    public async Task SignOut_ClearsTheMarkerWithTheSameScopeSoTheBrowserMatchesIt()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(Email, Password);
        using var browser = factory.CreateHttpsClient();
        await SignInAsync(browser, Email, Password);

        var csrf = await (await browser.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("X-CSRF-TOKEN", csrf!.RequestToken);
        var logout = await browser.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        var cleared = MarkerHeader(logout);
        Assert.NotNull(cleared);
        var lower = cleared!.ToLowerInvariant();
        Assert.StartsWith($"{MarkerName}=;", lower);
        Assert.Contains("expires=thu, 01 jan 1970", lower);
        Assert.Contains("path=/", lower);
        Assert.Contains("secure", lower);
        Assert.Contains("samesite=lax", lower);
    }

    [Fact]
    public async Task TheMarkerAloneGrantsNothing()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(Email, Password);
        using var forger = factory.CreateHttpsClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Cookie", $"{MarkerName}=1");

        var response = await forger.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string? MarkerHeader(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.SingleOrDefault(value => value.StartsWith($"{MarkerName}=", StringComparison.Ordinal))
            : null;

    private static async Task<HttpResponseMessage> SignInAsync(HttpClient client, string email, string password)
    {
        var csrf = await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.RequestToken);
        return await client.SendAsync(request);
    }
}
