namespace GooseWebsite.Api.Shared.Modules;

/// <summary>
/// Runs the startup hooks that modules register, keeping <c>Program.cs</c> module-agnostic.
/// </summary>
public static class ModuleStartupExtensions
{
    /// <summary>Runs every registered <see cref="IModuleInitializer"/> in registration order.</summary>
    public static async Task InitializeModulesAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        foreach (var initializer in scope.ServiceProvider.GetServices<IModuleInitializer>())
        {
            await initializer.InitializeAsync(scope.ServiceProvider);
        }
    }

    /// <summary>
    /// Offers the command-line arguments to every registered <see cref="IModuleCommand"/>.
    /// Returns true when a command handled them and the web host should not start.
    /// </summary>
    public static async Task<bool> TryRunModuleCommandAsync(this WebApplication app, string[] args)
    {
        foreach (var command in app.Services.GetServices<IModuleCommand>())
        {
            if (await command.TryRunAsync(args))
            {
                return true;
            }
        }

        return false;
    }
}
