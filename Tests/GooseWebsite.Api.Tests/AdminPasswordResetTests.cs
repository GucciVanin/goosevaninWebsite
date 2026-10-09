using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using GooseWebsite.Api.Modules.Accounts.Services;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Protects the operator-only path that restores access to an administrator account (F3-US3).
public sealed class AdminPasswordResetTests
{
    private const string AdminEmail = "admin@example.test";
    private const string OtherAdminEmail = "second-admin@example.test";
    private const string ReaderEmail = "reader@example.test";
    private const string OldPassword = "Strong-password-123";
    private const string NewPassword = "Brand-new-password-456";

    [Fact]
    public async Task Reset_ChangesOnlyThatAdministratorsPasswordAndEndsItsSessions()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, OldPassword);
        await factory.ProvisionAdministratorAsync(OtherAdminEmail, OldPassword);
        using var signedIn = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await SignInAsync(signedIn, AdminEmail, OldPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await signedIn.GetAsync("/api/auth/me")).StatusCode);

        var result = await ResetAsync(factory, AdminEmail, NewPassword);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(HttpStatusCode.Unauthorized, (await signedIn.GetAsync("/api/auth/me")).StatusCode);
        using var fresh = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await SignInAsync(fresh, AdminEmail, OldPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SignInAsync(fresh, AdminEmail, NewPassword)).StatusCode);
        using var other = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await SignInAsync(other, OtherAdminEmail, OldPassword)).StatusCode);
    }

    [Fact]
    public async Task Reset_RefusesUnknownEmailsAndReaderAccountsWithoutChangingAnything()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, OldPassword);
        await factory.ProvisionStandardAccountAsync(ReaderEmail, OldPassword);

        var unknown = await ResetAsync(factory, "nobody@example.test", NewPassword);
        var reader = await ResetAsync(factory, ReaderEmail, NewPassword);

        Assert.False(unknown.Succeeded);
        Assert.False(reader.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(unknown.ErrorMessage));
        Assert.False(string.IsNullOrWhiteSpace(reader.ErrorMessage));
        using var readerClient = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await SignInAsync(readerClient, ReaderEmail, OldPassword)).StatusCode);
        using var unknownClient = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await SignInAsync(unknownClient, "nobody@example.test", NewPassword)).StatusCode);
    }

    [Fact]
    public async Task Reset_RejectsAPasswordThatBreaksThePolicyAndKeepsTheOldOne()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, OldPassword);

        var result = await ResetAsync(factory, AdminEmail, "short");

        Assert.False(result.Succeeded);
        using var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await SignInAsync(client, AdminEmail, OldPassword)).StatusCode);
    }

    [Fact]
    public async Task Reset_LetsALockedOutAdministratorSignInWithTheNewPassword()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, OldPassword);
        using var client = factory.CreateHttpsClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await SignInAsync(client, AdminEmail, "Wrong-password-123");
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await SignInAsync(client, AdminEmail, OldPassword)).StatusCode);

        var result = await ResetAsync(factory, AdminEmail, NewPassword);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(HttpStatusCode.OK, (await SignInAsync(client, AdminEmail, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task Command_NeverTakesAPasswordAsAnArgument()
    {
        var command = new AdminPasswordResetCommand(scopeFactory: null!);

        Assert.False(await command.TryRunAsync(["admin", "reset-password", "Some-password-123"]));
        Assert.False(await command.TryRunAsync(["admin", "provision"]));
    }

    private static async Task<AdminPasswordResetResult> ResetAsync(AuthApiFactory factory, string email, string newPassword)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AdminPasswordResetService>().ResetAsync(email, newPassword);
    }

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
