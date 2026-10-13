using GooseWebsite.Api.Modules.Accounts.Contracts;
using GooseWebsite.Api.Modules.Accounts.Domain;
using GooseWebsite.Api.Shared.Hosting;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Accounts.Services;

// Owns the Reader registration and Verification rules so the controller only maps outcomes to responses.
public sealed class ReaderAccountService(
    DatabaseService database,
    IAccountEmailSender accountEmailSender,
    PublicLinkBuilder links,
    IConfiguration configuration) : IReaderAccountService
{
    public bool RegistrationEnabled => configuration.GetValue("Auth:ReaderRegistration:Enabled", false);

    public async Task<ReaderRegistrationOutcome> RegisterAsync(RegisterRequest request, bool inputIsValid)
    {
        if (!RegistrationEnabled)
        {
            return ReaderRegistrationOutcome.Disabled;
        }

        if (!inputIsValid || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return ReaderRegistrationOutcome.InvalidInput;
        }

        var email = request.Email.Trim();
        if (await database.FindUserByEmailAsync(email) is not null)
        {
            return ReaderRegistrationOutcome.Accepted;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            EmailConfirmed = false
        };

        if (!(await database.CreateUserAsync(user, request.Password)).Succeeded)
        {
            return ReaderRegistrationOutcome.Accepted;
        }

        if (!await TryAssignReaderRoleAsync(user))
        {
            await database.DeleteUserAsync(user);
            return ReaderRegistrationOutcome.Accepted;
        }

        await SendVerificationEmailAsync(user);
        return ReaderRegistrationOutcome.Accepted;
    }

    public async Task ResendVerificationAsync(string email)
    {
        var user = await database.FindUserByEmailAsync(email.Trim());
        if (user is not null
            && !user.EmailConfirmed
            && await database.IsUserInRoleAsync(user, AccountRoles.Reader))
        {
            await SendVerificationEmailAsync(user);
        }
    }

    public async Task<bool> VerifyEmailAsync(string userId, string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var user = await database.FindUserByIdAsync(userId);
        if (user is null || user.EmailConfirmed)
        {
            return false;
        }

        return (await database.ConfirmUserEmailAsync(user, token)).Succeeded;
    }

    // Identity throws (instead of returning a failed result) when the role itself does not exist.
    private async Task<bool> TryAssignReaderRoleAsync(ApplicationUser user)
    {
        try
        {
            return (await database.AddUserToRoleAsync(user, AccountRoles.Reader)).Succeeded;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private async Task SendVerificationEmailAsync(ApplicationUser user)
    {
        var token = await database.GenerateUserEmailConfirmationTokenAsync(user);
        var verificationUrl = links.FragmentLink("/verify-email", ("userId", user.Id), ("token", token));
        await accountEmailSender.SendVerificationEmailAsync(user.Email!, user.DisplayName, verificationUrl);
    }
}
