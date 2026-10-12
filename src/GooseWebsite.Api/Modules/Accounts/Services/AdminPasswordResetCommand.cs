using GooseWebsite.Api.Shared.Modules;

namespace GooseWebsite.Api.Modules.Accounts.Services;

// Owns the interactive console workflow (admin reset-password) for restoring administrator access.
public sealed class AdminPasswordResetCommand(IServiceScopeFactory scopeFactory) : IModuleCommand
{
    public async Task<bool> TryRunAsync(string[] args)
    {
        if (args.Length != 2 || args[0] != "admin" || args[1] != "reset-password")
        {
            return false;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var resetter = scope.ServiceProvider.GetRequiredService<AdminPasswordResetService>();
        var email = ConsolePrompt.Text("Administrator email: ");
        var password = ConsolePrompt.Secret("New password: ");
        var confirmation = ConsolePrompt.Secret("Confirm new password: ");
        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Passwords do not match.");
            Environment.ExitCode = 1;
            return true;
        }

        var result = await resetter.ResetAsync(email, password);
        if (!result.Succeeded)
        {
            Console.Error.WriteLine(result.ErrorMessage);
            Environment.ExitCode = 1;
            return true;
        }

        Console.WriteLine("Administrator password changed. Existing sessions for that account have ended.");
        return true;
    }
}
