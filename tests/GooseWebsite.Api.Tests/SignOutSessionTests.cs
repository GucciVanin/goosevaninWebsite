using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F3-US1 acceptance test 3: after signing out, "the same session" must not work again, even if its cookie was
// captured before sign-out. Found by the container verification run (TE10): sign-out used to clear only the browser's copy.
public sealed class SignOutSessionTests
{
    private const string Email = "signout-admin@example.test";
    private const string Password = "Strong-password-123";

    [Fact]
    public async Task SignOut_EndsTheSessionEvenForACookieCapturedBeforehand()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(Email, Password);
        using var browser = factory.CreateHttpsClient();
        var capturedCookie = await SignInAndCaptureCookieAsync(browser);

        using var replay = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false
        });
        Assert.Equal(HttpStatusCode.OK, (await GetMeWithCookieAsync(replay, capturedCookie)).StatusCode);

        var logout = await PostWithCsrfAsync(browser, "/api/auth/logout");
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMeWithCookieAsync(replay, capturedCookie)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task SignOut_DoesNotStopTheAccountFromSigningInAgain()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(Email, Password);
        using var browser = factory.CreateHttpsClient();
        await SignInAndCaptureCookieAsync(browser);
        await PostWithCsrfAsync(browser, "/api/auth/logout");

        var again = await SignInAndCaptureCookieAsync(browser);

        Assert.False(string.IsNullOrWhiteSpace(again));
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/api/auth/me")).StatusCode);
    }

    private static async Task<string> SignInAndCaptureCookieAsync(HttpClient client)
    {
        var csrf = await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = Email, password = Password })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.RequestToken);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var setCookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("goosewebsite.auth=", StringComparison.Ordinal));
        return setCookie.Split(';')[0];
    }

    private static async Task<HttpResponseMessage> PostWithCsrfAsync(HttpClient client, string path)
    {
        var csrf = await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>();
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("X-CSRF-TOKEN", csrf!.RequestToken);
        return await client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> GetMeWithCookieAsync(HttpClient client, string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Cookie", cookie);
        return client.SendAsync(request);
    }
}
