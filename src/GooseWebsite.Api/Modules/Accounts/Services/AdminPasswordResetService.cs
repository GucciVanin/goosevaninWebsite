using GooseWebsite.Api.Modules.Accounts.Domain;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Accounts.Services;

// Restores access to an existing administrator account. Only reachable through the operator command.
public sealed class AdminPasswordResetService(DatabaseService database)
{
    // One message for both refusals, so the operator output never describes other accounts.
    private const string NotAnAdministrator = "No administrator account exists for that email.";

    public async Task<AdminPasswordResetResult> ResetAsync(string email, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(newPassword))
        {
            return AdminPasswordResetResult.Failure("Email and password are required.");
        }

        var user = await database.FindUserByEmailAsync(email.Trim());
        if (user is null || !await database.IsUserInRoleAsync(user, AccountRoles.Admin))
        {
            return AdminPasswordResetResult.Failure(NotAnAdministrator);
        }

        var result = await database.ResetUserPasswordAsync(user, newPassword);
        return result.Succeeded
            ? AdminPasswordResetResult.Success()
            : AdminPasswordResetResult.Failure(
                $"The password could not be changed ({string.Join(", ", result.Errors.Select(error => error.Code))}).");
    }
}
