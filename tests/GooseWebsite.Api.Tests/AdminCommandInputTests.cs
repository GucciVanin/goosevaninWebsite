using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using GooseWebsite.Api.Modules.Accounts.Services;
using Xunit;

namespace GooseWebsite.Api.Tests;

// The operator commands can be driven by a script: answers piped in on standard input, never as arguments (TE10).
// They set Environment.ExitCode on failure, so these tests run one at a time and restore it.
[Collection("OperatorCommands")]
public sealed class AdminCommandInputTests : IDisposable
{
    public AdminCommandInputTests() => Environment.ExitCode = 0;

    public void Dispose() => Environment.ExitCode = 0;

    private const string Email = "scripted-admin@example.test";
    private const string OldPassword = "Strong-password-123";
    private const string NewPassword = "Brand-new-password-456";

    [Fact]
    public async Task Provision_ReadsEmailPasswordAndConfirmationFromPipedInput()
    {
        using var factory = new AuthApiFactory();
        var command = new AdminProvisioningCommand(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            new StringReader($"{Email}\n{OldPassword}\n{OldPassword}\n"));

        var handled = await command.TryRunAsync(["admin", "provision"]);

        Assert.True(handled);
        Assert.Equal(0, Environment.ExitCode);
        using var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await SignInAsync(client, Email, OldPassword)).StatusCode);
    }

    [Fact]
    public async Task Provision_IgnoresAByteOrderMarkAtTheStartOfPipedInput()
    {
        // Windows PowerShell 5.1 puts a UTF-8 byte-order mark in front of text piped to a program.
        using var factory = new AuthApiFactory();
        var command = new AdminProvisioningCommand(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            new StringReader($"﻿{Email}\n{OldPassword}\n{OldPassword}\n"));

        var handled = await command.TryRunAsync(["admin", "provision"]);

        Assert.True(handled);
        Assert.Equal(0, Environment.ExitCode);
        using var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await SignInAsync(client, Email, OldPassword)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ReadsEmailPasswordAndConfirmationFromPipedInput()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(Email, OldPassword);
        var command = new AdminPasswordResetCommand(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            new StringReader($"{Email}\n{NewPassword}\n{NewPassword}\n"));

        var handled = await command.TryRunAsync(["admin", "reset-password"]);

        Assert.True(handled);
        Assert.Equal(0, Environment.ExitCode);
        using var oldClient = factory.CreateHttpsClient();
        using var newClient = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await SignInAsync(oldClient, Email, OldPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SignInAsync(newClient, Email, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithAMismatchedConfirmation_FailsAndChangesNothing()
    {
        using var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(Email, OldPassword);
        var command = new AdminPasswordResetCommand(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            new StringReader($"{Email}\n{NewPassword}\nsomething-else-entirely\n"));

        try
        {
            var handled = await command.TryRunAsync(["admin", "reset-password"]);

            Assert.True(handled);
            Assert.Equal(1, Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode = 0;
        }

        using var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await SignInAsync(client, Email, OldPassword)).StatusCode);
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
