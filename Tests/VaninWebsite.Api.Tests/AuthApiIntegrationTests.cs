using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VaninWebsite.Api.Modules.Accounts.Domain;
using VaninWebsite.Api.Modules.Accounts.Services;
using VaninWebsite.Api.Shared.Persistence;
using Xunit;

namespace VaninWebsite.Api.Tests;

// Verifies authentication behavior across the real HTTP pipeline and identity store.
public sealed class AuthApiIntegrationTests
{
    private const string AdminEmail = "admin@example.test";
    private const string AdminPassword = "Strong-password-123";

    [Fact]
    public async Task AdminLoginRequiresCsrfAndSetsSecureHttpOnlySessionCookie()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, AdminPassword);
        using var client = factory.CreateHttpsClient();
        var csrfResponse = await client.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        var csrfCookie = string.Join(";", csrfResponse.Headers.GetValues("Set-Cookie"));
        Assert.Contains("vaninwebsite.csrf=", csrfCookie);
        Assert.Contains("httponly", csrfCookie.ToLowerInvariant());
        Assert.Contains("secure", csrfCookie.ToLowerInvariant());
        Assert.Contains("samesite=lax", csrfCookie.ToLowerInvariant());
        var csrfPayload = await csrfResponse.Content.ReadFromJsonAsync<CsrfResponse>();
        Assert.False(string.IsNullOrWhiteSpace(csrfPayload?.RequestToken));
        var csrf = csrfPayload!.RequestToken;

        var missingTokenResponse = await client.PostAsJsonAsync("/api/auth/login", new { email = AdminEmail, password = AdminPassword });
        Assert.Equal(HttpStatusCode.BadRequest, missingTokenResponse.StatusCode);

        csrf = await GetCsrfTokenAsync(client);
        var loginResponse = await PostWithCsrfAsync(client, "/api/auth/login", csrf, new { email = AdminEmail, password = AdminPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authCookie = string.Join(";", loginResponse.Headers.GetValues("Set-Cookie"));
        Assert.Contains("vaninwebsite.auth=", authCookie);
        Assert.Contains("httponly", authCookie.ToLowerInvariant());
        Assert.Contains("secure", authCookie.ToLowerInvariant());
        Assert.Contains("samesite=lax", authCookie.ToLowerInvariant());
        Assert.DoesNotContain("expires=", authCookie.ToLowerInvariant());

        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        using var me = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        Assert.Equal("Site administrator", me.RootElement.GetProperty("displayName").GetString());
        Assert.True(me.RootElement.GetProperty("isAdmin").GetBoolean());
        Assert.False(me.RootElement.TryGetProperty("email", out _));
    }

    [Fact]
    public async Task InvalidAndUnknownCredentialsHaveSamePublicResponse()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, AdminPassword);
        using var client = factory.CreateHttpsClient();
        var csrf = await GetCsrfTokenAsync(client);

        var unknownResponse = await PostWithCsrfAsync(client, "/api/auth/login", csrf, new { email = "unknown@example.test", password = "Wrong-password-123" });
        var wrongPasswordResponse = await PostWithCsrfAsync(client, "/api/auth/login", csrf, new { email = AdminEmail, password = "Wrong-password-123" });
        for (var attempt = 1; attempt < 5; attempt++)
        {
            await PostWithCsrfAsync(client, "/api/auth/login", csrf, new { email = AdminEmail, password = "Wrong-password-123" });
        }

        var lockedResponse = await PostWithCsrfAsync(client, "/api/auth/login", csrf, new { email = AdminEmail, password = AdminPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, unknownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, lockedResponse.StatusCode);
        Assert.Equal(await unknownResponse.Content.ReadAsStringAsync(), await wrongPasswordResponse.Content.ReadAsStringAsync());
        Assert.Equal(await unknownResponse.Content.ReadAsStringAsync(), await lockedResponse.Content.ReadAsStringAsync());
        var issuedCookies = wrongPasswordResponse.Headers.TryGetValues("Set-Cookie", out var cookieValues)
            ? string.Join(";", cookieValues)
            : string.Empty;
        Assert.DoesNotContain("vaninwebsite.auth=", issuedCookies);
    }

    [Fact]
    public async Task SharedLoginAuthenticatesStandardAccountAndBackendReportsAccessLevel()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionStandardAccountAsync();
        using var client = factory.CreateHttpsClient();
        var csrf = await GetCsrfTokenAsync(client);

        var login = await PostWithCsrfAsync(client, "/api/auth/login", csrf, new { email = "reader@example.test", password = AdminPassword });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginBody = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.False(loginBody.RootElement.GetProperty("isAdmin").GetBoolean());

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var meBody = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal("Test reader", meBody.RootElement.GetProperty("displayName").GetString());
        Assert.False(meBody.RootElement.GetProperty("isAdmin").GetBoolean());
    }

    [Fact]
    public async Task AdminMutationsRequireCsrfAndLogoutInvalidatesSession()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, AdminPassword);
        using var client = factory.CreateHttpsClient();
        var anonymousWrite = await client.PostAsJsonAsync("/api/blog", BlogPost());
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousWrite.StatusCode);

        var csrf = await GetCsrfTokenAsync(client);
        var login = await PostWithCsrfAsync(client, "/api/auth/login", csrf, new { email = AdminEmail, password = AdminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var missingMutationToken = await client.PostAsJsonAsync("/api/blog", BlogPost());
        Assert.Equal(HttpStatusCode.BadRequest, missingMutationToken.StatusCode);
        csrf = await GetCsrfTokenAsync(client);
        var created = await PostWithCsrfAsync(client, "/api/blog", csrf, BlogPost());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var logoutCsrf = await GetCsrfTokenAsync(client);
        var logout = await PostWithCsrfAsync(client, "/api/auth/logout", logoutCsrf, new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/blog", BlogPost())).StatusCode);
    }

    [Fact]
    public async Task ExpiredAdminSessionCannotAccessProtectedEndpoints()
    {
        using var factory = new AuthApiFactory(TimeSpan.FromMilliseconds(150));
        await factory.ProvisionAdministratorAsync(AdminEmail, AdminPassword);
        using var client = factory.CreateHttpsClient();
        var csrf = await GetCsrfTokenAsync(client);
        var login = await PostWithCsrfAsync(client, "/api/auth/login", csrf, new { email = AdminEmail, password = AdminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        await Task.Delay(1200);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/blog", BlogPost())).StatusCode);
    }

    [Fact]
    public async Task ReaderRegistrationRequiresEmailConfirmationBeforeSignIn()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();
        var csrf = await GetCsrfTokenAsync(client);

        var registration = await PostWithCsrfAsync(client, "/api/auth/register", csrf, new
        {
            email = "reader@example.test",
            password = "Strong-password-123",
            displayName = "Reader One"
        });

        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        var registrationBody = await registration.Content.ReadAsStringAsync();
        Assert.Contains("eligible", registrationBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reader@example.test", registrationBody, StringComparison.OrdinalIgnoreCase);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync("reader@example.test");
        Assert.NotNull(user);
        Assert.Equal("Reader One", user!.DisplayName);
        Assert.False(user.EmailConfirmed);
        Assert.True(await users.IsInRoleAsync(user, "Reader"));
        var unverifiedPrincipalFactory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var unverifiedPrincipal = await unverifiedPrincipalFactory.CreateAsync(user);
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        Assert.False((await authorization.AuthorizeAsync(unverifiedPrincipal, "VerifiedReader")).Succeeded);

        var sentMessage = Assert.Single(factory.AccountEmailSender.SentMessages);
        Assert.Equal(user.Email, sentMessage.Email);
        Assert.Equal(user.DisplayName, sentMessage.DisplayName);
        Assert.Contains("/verify-email#", sentMessage.Url, StringComparison.Ordinal);
        Assert.Empty(new Uri(sentMessage.Url).Query);
        var verificationQuery = HttpUtility.ParseQueryString(new Uri(sentMessage.Url).Fragment.TrimStart('#'));
        var userId = verificationQuery["userId"]!;
        var token = verificationQuery["token"]!;

        var unverifiedLogin = await PostWithCsrfAsync(client, "/api/auth/login", csrf, new
        {
            email = "reader@example.test",
            password = "Strong-password-123"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, unverifiedLogin.StatusCode);
        Assert.False(unverifiedLogin.Headers.TryGetValues("Set-Cookie", out var cookies)
            && cookies.Any(cookie => cookie.Contains("vaninwebsite.auth", StringComparison.OrdinalIgnoreCase)));

        var invalidVerification = await PostWithCsrfAsync(client, "/api/auth/verify-email", csrf, new
        {
            userId,
            token = "invalid-token"
        });
        Assert.Equal(HttpStatusCode.OK, invalidVerification.StatusCode);
        Assert.Contains("invalid or expired", await invalidVerification.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.False((await users.FindByEmailAsync("reader@example.test"))!.EmailConfirmed);

        var verificationPage = await client.GetAsync(new Uri(sentMessage.Url).AbsolutePath);
        Assert.Equal(HttpStatusCode.OK, verificationPage.StatusCode);
        Assert.Equal("no-referrer", verificationPage.Headers.GetValues("Referrer-Policy").Single());
        Assert.False((await users.FindByEmailAsync("reader@example.test"))!.EmailConfirmed);

        var verification = await PostWithCsrfAsync(client, "/api/auth/verify-email", csrf, new { userId, token });

        Assert.Equal(HttpStatusCode.OK, verification.StatusCode);
        Assert.Contains("verified", await verification.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        using var verificationScope = factory.Services.CreateScope();
        var verificationUsers = verificationScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var confirmedUser = await verificationUsers.FindByIdAsync(user.Id);
        Assert.NotNull(confirmedUser);
        Assert.True(confirmedUser!.EmailConfirmed);
        using var refreshedAuthorizationScope = factory.Services.CreateScope();
        var refreshedAuthorization = refreshedAuthorizationScope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        Assert.True((await refreshedAuthorization.AuthorizeAsync(unverifiedPrincipal, "VerifiedReader")).Succeeded);
        var confirmedPrincipalFactory = verificationScope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var confirmedPrincipal = await confirmedPrincipalFactory.CreateAsync(confirmedUser);
        var verificationAuthorization = verificationScope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        Assert.True((await verificationAuthorization.AuthorizeAsync(confirmedPrincipal, "VerifiedReader")).Succeeded);

        var replayedVerification = await PostWithCsrfAsync(client, "/api/auth/verify-email", csrf, new { userId, token });
        Assert.Equal(HttpStatusCode.OK, replayedVerification.StatusCode);
        Assert.Contains("invalid or expired", await replayedVerification.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var confirmedLogin = await PostWithCsrfAsync(client, "/api/auth/login", csrf, new
        {
            email = "reader@example.test",
            password = "Strong-password-123"
        });
        Assert.Equal(HttpStatusCode.OK, confirmedLogin.StatusCode);
        var signedInResponse = await confirmedLogin.Content.ReadAsStringAsync();
        Assert.Contains("Reader One", signedInResponse, StringComparison.Ordinal);
        Assert.DoesNotContain("reader@example.test", signedInResponse, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"role\"", signedInResponse, StringComparison.OrdinalIgnoreCase);

        var logoutCsrf = await GetCsrfTokenAsync(client);
        var logout = await PostWithCsrfAsync(client, "/api/auth/logout", logoutCsrf, new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task RegistrationAndVerificationResendResponsesDoNotRevealAccountExistence()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();
        var csrf = await GetCsrfTokenAsync(client);
        var registrationRequest = new
        {
            email = "reader@example.test",
            password = "Strong-password-123",
            displayName = "Reader One"
        };

        var firstRegistration = await PostWithCsrfAsync(client, "/api/auth/register", csrf, registrationRequest);
        var duplicateRegistration = await PostWithCsrfAsync(client, "/api/auth/register", csrf, registrationRequest);
        Assert.Equal(firstRegistration.StatusCode, duplicateRegistration.StatusCode);
        Assert.Equal(await firstRegistration.Content.ReadAsStringAsync(), await duplicateRegistration.Content.ReadAsStringAsync());
        Assert.Single(factory.AccountEmailSender.SentMessages);

        var resendExisting = await PostWithCsrfAsync(client, "/api/auth/verification/resend", csrf, new { email = "reader@example.test" });
        var resendUnknown = await PostWithCsrfAsync(client, "/api/auth/verification/resend", csrf, new { email = "unknown@example.test" });
        Assert.Equal(resendExisting.StatusCode, resendUnknown.StatusCode);
        Assert.Equal(await resendExisting.Content.ReadAsStringAsync(), await resendUnknown.Content.ReadAsStringAsync());
        Assert.Equal(2, factory.AccountEmailSender.SentMessages.Count);
        Assert.DoesNotContain("unknown@example.test", await resendUnknown.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExpiredReaderVerificationTokenDoesNotConfirmAccount()
    {
        using var factory = new AuthApiFactory(confirmationTokenLifetime: TimeSpan.FromSeconds(1));
        using var client = factory.CreateHttpsClient();
        var csrf = await GetCsrfTokenAsync(client);
        await PostWithCsrfAsync(client, "/api/auth/register", csrf, new
        {
            email = "expired-reader@example.test",
            password = "Strong-password-123",
            displayName = "Expired Reader"
        });
        var sentMessage = Assert.Single(factory.AccountEmailSender.SentMessages);
        var verificationQuery = HttpUtility.ParseQueryString(new Uri(sentMessage.Url).Fragment.TrimStart('#'));
        await Task.Delay(TimeSpan.FromSeconds(1.2));

        var verification = await PostWithCsrfAsync(client, "/api/auth/verify-email", csrf, new
        {
            userId = verificationQuery["userId"],
            token = verificationQuery["token"]
        });

        Assert.Equal(HttpStatusCode.OK, verification.StatusCode);
        Assert.Contains("invalid or expired", await verification.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync("expired-reader@example.test");
        Assert.NotNull(user);
        Assert.False(user!.EmailConfirmed);
    }

    [Fact]
    public async Task FailedVerificationEmailCanBeRecoveredByResending()
    {
        using var factory = new AuthApiFactory();
        factory.AccountEmailSender.ShouldFail = true;
        using var client = factory.CreateHttpsClient();
        var csrf = await GetCsrfTokenAsync(client);
        var registration = await PostWithCsrfAsync(client, "/api/auth/register", csrf, new
        {
            email = "retry-reader@example.test",
            password = "Strong-password-123",
            displayName = "Retry Reader"
        });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync("retry-reader@example.test");
        Assert.NotNull(user);
        Assert.False(user!.EmailConfirmed);
        Assert.Empty(factory.AccountEmailSender.SentMessages);

        factory.AccountEmailSender.ShouldFail = false;
        var resend = await PostWithCsrfAsync(client, "/api/auth/verification/resend", csrf, new { email = "retry-reader@example.test" });
        Assert.Equal(HttpStatusCode.OK, resend.StatusCode);
        Assert.Single(factory.AccountEmailSender.SentMessages);
        Assert.False((await users.FindByEmailAsync("retry-reader@example.test"))!.EmailConfirmed);
    }

    [Fact]
    public async Task ReaderRegistrationIsRateLimitedPerClient()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();
        var csrf = await GetCsrfTokenAsync(client);
        var statuses = new List<HttpStatusCode>();

        for (var requestNumber = 0; requestNumber < 6; requestNumber++)
        {
            var response = await PostWithCsrfAsync(client, "/api/auth/register", csrf, new
            {
                email = $"limited-reader-{requestNumber}@example.test",
                password = "Strong-password-123",
                displayName = $"Reader {requestNumber}"
            });
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(5), status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[5]);
        Assert.Equal(5, factory.AccountEmailSender.SentMessages.Count);
    }

    [Fact]
    public async Task ReaderRegistrationIsDisabledUnlessExplicitlyEnabled()
    {
        using var factory = new AuthApiFactory(readerRegistrationEnabled: false);
        using var client = factory.CreateHttpsClient();
        var availability = await client.GetFromJsonAsync<JsonElement>("/api/auth/reader-registration");
        Assert.False(availability.GetProperty("enabled").GetBoolean());
        var csrf = await GetCsrfTokenAsync(client);

        var registration = await PostWithCsrfAsync(client, "/api/auth/register", csrf, new
        {
            email = "disabled-reader@example.test",
            password = "Strong-password-123",
            displayName = "Disabled Reader"
        });

        Assert.Equal(HttpStatusCode.NotFound, registration.StatusCode);
        Assert.Empty(factory.AccountEmailSender.SentMessages);
    }

    [Fact]
    public async Task PublicRegistrationIgnoresRequestedAdminRoleAndCreatesReaderAccount()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();
        var csrf = await GetCsrfTokenAsync(client);

        var response = await PostWithCsrfAsync(client, "/api/auth/register", csrf, new
        {
            email = "attacker@example.test",
            password = AdminPassword,
            displayName = "Attacker",
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync("attacker@example.test");
        Assert.NotNull(user);
        Assert.Equal("Attacker", user!.DisplayName);
        Assert.False(user.EmailConfirmed);
        Assert.True(await users.IsInRoleAsync(user, "Reader"));
        Assert.False(await users.IsInRoleAsync(user, "Admin"));
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CsrfResponse>();
        Assert.False(string.IsNullOrWhiteSpace(payload?.RequestToken));
        return payload!.RequestToken;
    }

    private static Task<HttpResponseMessage> PostWithCsrfAsync(HttpClient client, string path, string token, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return client.SendAsync(request);
    }

    private static object BlogPost() => new
    {
        title = "Auth test post",
        excerpt = "A post created by an auth integration test.",
        content = "test content",
        category = "Testing",
        tags = new[] { "auth" },
        isPublished = true
    };

}