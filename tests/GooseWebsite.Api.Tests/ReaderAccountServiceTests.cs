using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using GooseWebsite.Api.Modules.Accounts.Contracts;
using GooseWebsite.Api.Modules.Accounts.Domain;
using GooseWebsite.Api.Modules.Accounts.Services;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Exercises Reader registration through its own interface. Request-level behavior (identical responses,
// verification link, sign-in rules) stays covered by AuthApiIntegrationTests.
public sealed class ReaderAccountServiceTests
{
    private static RegisterRequest NewReader(string email = "reader@example.test") => new()
    {
        Email = email,
        Password = "Strong-password-123",
        DisplayName = "Reader One"
    };

    [Fact]
    public async Task Register_WhenReaderRegistrationIsOff_IsRefused()
    {
        using var factory = new AuthApiFactory(readerRegistrationEnabled: false);
        using var scope = factory.Services.CreateScope();
        var readers = scope.ServiceProvider.GetRequiredService<IReaderAccountService>();

        var outcome = await readers.RegisterAsync(NewReader(), inputIsValid: true);

        Assert.Equal(ReaderRegistrationOutcome.Disabled, outcome);
        Assert.Empty(factory.Mail.AccountMessages);
    }

    [Fact]
    public async Task Register_WhenTheReaderRoleCannotBeAssigned_LeavesNoHalfCreatedAccountAndSendsNothing()
    {
        using var factory = new AuthApiFactory();
        using var scope = factory.Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var readerRole = await roles.FindByNameAsync(AccountRoles.Reader);
        Assert.True((await roles.DeleteAsync(readerRole!)).Succeeded);
        var readers = scope.ServiceProvider.GetRequiredService<IReaderAccountService>();

        var outcome = await readers.RegisterAsync(NewReader(), inputIsValid: true);

        Assert.Equal(ReaderRegistrationOutcome.Accepted, outcome);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await users.FindByEmailAsync("reader@example.test"));
        Assert.Empty(factory.Mail.AccountMessages);
    }

    [Fact]
    public async Task Register_AnswersTheSameWhetherOrNotTheAddressIsAlreadyRegistered()
    {
        using var factory = new AuthApiFactory();
        using var scope = factory.Services.CreateScope();
        var readers = scope.ServiceProvider.GetRequiredService<IReaderAccountService>();

        var first = await readers.RegisterAsync(NewReader(), inputIsValid: true);
        var again = await readers.RegisterAsync(NewReader(), inputIsValid: true);

        Assert.Equal(first, again);
        Assert.Single(factory.Mail.AccountMessages);
    }

    [Fact]
    public async Task VerifyEmail_ConfirmsTheAddressOnceAndRefusesAnInvalidToken()
    {
        using var factory = new AuthApiFactory();
        using var scope = factory.Services.CreateScope();
        var readers = scope.ServiceProvider.GetRequiredService<IReaderAccountService>();
        await readers.RegisterAsync(NewReader(), inputIsValid: true);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByEmailAsync("reader@example.test"))!;
        var token = await users.GenerateEmailConfirmationTokenAsync(user);

        Assert.False(await readers.VerifyEmailAsync(user.Id, "invalid-token"));
        Assert.True(await readers.VerifyEmailAsync(user.Id, token));
        Assert.False(await readers.VerifyEmailAsync(user.Id, token));
    }
}
