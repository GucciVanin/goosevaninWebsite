using VaninWebsite.Api.Modules.Accounts.Domain;
using VaninWebsite.Api.Shared.Modules;
using VaninWebsite.Api.Shared.Persistence;

namespace VaninWebsite.Api.Modules.Accounts.Services;

/// <summary>Ensures the server-managed roles exist at startup. Never creates users.</summary>
public sealed class AccountRoleInitializer : IModuleInitializer
{
    public async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var database = services.GetRequiredService<DatabaseService>();
        foreach (var role in AccountRoles.All)
        {
            if (await database.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await database.CreateRoleAsync(role);
            if (!result.Succeeded && !await database.RoleExistsAsync(role))
            {
                throw new InvalidOperationException($"The '{role}' role could not be initialized.");
            }
        }
    }
}
