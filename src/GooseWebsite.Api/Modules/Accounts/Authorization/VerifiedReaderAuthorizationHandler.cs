using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using GooseWebsite.Api.Shared.Persistence;
using GooseWebsite.Api.Modules.Accounts.Domain;

namespace GooseWebsite.Api.Modules.Accounts.Authorization;

// Reads current persisted confirmation state so stale cookie claims cannot authorize readers.
public sealed class VerifiedReaderAuthorizationHandler(DatabaseService database)
    : AuthorizationHandler<VerifiedReaderRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        VerifiedReaderRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var user = await database.FindUserByIdAsync(userId);
        if (user is { EmailConfirmed: true } && await database.IsUserInRoleAsync(user, AccountRoles.Reader))
        {
            context.Succeed(requirement);
        }
    }
}
