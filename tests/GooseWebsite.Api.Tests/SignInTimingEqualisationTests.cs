using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GooseWebsite.Api.Modules.Accounts.Domain;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F3-US1: the sign-in code claims that an unknown email costs the same password-hash work as a known one (a dummy hash
// is verified), so response time does not reveal which emails exist. Time itself is not a stable thing to assert, so
// the test counts the hash verifications each kind of failed sign-in performs: they must be the same, and not zero.
public sealed class SignInTimingEqualisationTests
{
    private const string AdminEmail = "admin@example.test";
    private const string AdminPassword = "Strong-password-123";

    [Fact]
    public async Task UnknownEmailWrongPasswordAndLockedAccountEachRunTheSameNumberOfHashVerifications()
    {
        var hasher = new CountingPasswordHasher();
        using var factory = new AuthApiFactory(configureServices: services =>
        {
            services.RemoveAll<IPasswordHasher<ApplicationUser>>();
            services.AddSingleton<IPasswordHasher<ApplicationUser>>(hasher);
        });
        await factory.ProvisionAdministratorAsync(AdminEmail, AdminPassword);
        using var client = factory.CreateHttpsClient();
        var csrf = (await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken;

        var unknown = await CountVerificationsAsync(hasher, client, csrf, "unknown@example.test", "Wrong-password-123");
        var wrongPassword = await CountVerificationsAsync(hasher, client, csrf, AdminEmail, "Wrong-password-123");
        for (var attempt = 1; attempt < 5; attempt++)
        {
            await CountVerificationsAsync(hasher, client, csrf, AdminEmail, "Wrong-password-123");
        }

        var locked = await CountVerificationsAsync(hasher, client, csrf, AdminEmail, AdminPassword);

        Assert.True(unknown > 0, "an unknown email must still cost a password-hash verification (the dummy hash)");
        Assert.Equal(wrongPassword, unknown);
        Assert.Equal(wrongPassword, locked);
    }

    private static async Task<int> CountVerificationsAsync(
        CountingPasswordHasher hasher, HttpClient client, string csrf, string email, string password)
    {
        hasher.Reset();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        return hasher.Verifications;
    }

    private sealed class CountingPasswordHasher : IPasswordHasher<ApplicationUser>
    {
        private readonly PasswordHasher<ApplicationUser> _inner = new();
        private int _verifications;

        public int Verifications => Volatile.Read(ref _verifications);

        public void Reset() => Interlocked.Exchange(ref _verifications, 0);

        public string HashPassword(ApplicationUser user, string password) => _inner.HashPassword(user, password);

        public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
        {
            Interlocked.Increment(ref _verifications);
            return _inner.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }
    }
}
