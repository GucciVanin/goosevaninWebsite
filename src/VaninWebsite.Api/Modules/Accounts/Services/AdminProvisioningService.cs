using VaninWebsite.Api.Shared.Persistence;
using VaninWebsite.Api.Modules.Accounts.Domain;

namespace VaninWebsite.Api.Modules.Accounts.Services;

// Restricts privileged account creation to an explicit administrative operation.
public sealed class AdminProvisioningService(
    DatabaseService database)
{
    public async Task<AdminProvisioningResult> ProvisionAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AdminProvisioningResult.Failure("Email and password are required.");
        }

        var normalizedEmail = database.NormalizeEmail(email);
        if (normalizedEmail is null || await database.FindUserByEmailAsync(email) is not null)
        {
            return AdminProvisioningResult.Failure("An account with that email already exists or the email is invalid.");
        }

        if (!await database.RoleExistsAsync(AccountRoles.Admin))
        {
            var roleResult = await database.CreateRoleAsync(AccountRoles.Admin);
            if (!roleResult.Succeeded && !await database.RoleExistsAsync(AccountRoles.Admin))
            {
                return AdminProvisioningResult.Failure("The administrator role could not be initialized.");
            }
        }

        var user = new ApplicationUser
        {
            UserName = email.Trim(),
            Email = email.Trim(),
            DisplayName = "Site administrator",
            EmailConfirmed = true
        };

        var createResult = await database.CreateUserAsync(user, password);
        if (!createResult.Succeeded)
        {
            return AdminProvisioningResult.Failure(
                $"The administrator account could not be created ({string.Join(", ", createResult.Errors.Select(error => error.Code))}).");
        }

        var roleAssignment = await database.AddUserToRoleAsync(user, AccountRoles.Admin);
        if (roleAssignment.Succeeded)
        {
            return AdminProvisioningResult.Success();
        }

        var cleanupResult = await database.DeleteUserAsync(user);
        if (!cleanupResult.Succeeded)
        {
            return AdminProvisioningResult.Failure(
                "Administrator role assignment failed and account cleanup also failed; inspect the account before retrying.");
        }

        return AdminProvisioningResult.Failure("Administrator role assignment failed; the unprivileged account was removed.");
    }
}
