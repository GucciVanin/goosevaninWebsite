using GooseWebsite.Api.Shared.Modules;

namespace GooseWebsite.Api.Modules.Accounts.Services;

// Owns the interactive console workflow for deliberately provisioning administrators.
public sealed class AdminProvisioningCommand(IServiceScopeFactory scopeFactory, TextReader? input = null) : IModuleCommand
{
    private readonly ConsolePrompt prompt = new(input);

    public async Task<bool> TryRunAsync(string[] args)
    {
        if (args.Length != 2 || args[0] != "admin" || args[1] != "provision")
        {
            return false;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<AdminProvisioningService>();
        var email = prompt.Text("Administrator email: ");
        var password = prompt.Secret("Password: ");
        var confirmation = prompt.Secret("Confirm password: ");
        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Passwords do not match.");
            Environment.ExitCode = 1;
            return true;
        }

        var result = await provisioner.ProvisionAsync(email, password);
        if (!result.Succeeded)
        {
            Console.Error.WriteLine(result.ErrorMessage);
            Environment.ExitCode = 1;
            return true;
        }

        Console.WriteLine("Administrator account provisioned.");
        return true;
    }
}
