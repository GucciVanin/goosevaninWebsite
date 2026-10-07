using GooseWebsite.Api.Shared.Modules;

namespace GooseWebsite.Api.Modules.Accounts.Services;

// Owns the interactive console workflow for deliberately provisioning administrators.
public sealed class AdminProvisioningCommand(IServiceScopeFactory scopeFactory) : IModuleCommand
{
    public async Task<bool> TryRunAsync(string[] args)
    {
        if (args.Length != 2 || args[0] != "admin" || args[1] != "provision")
        {
            return false;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<AdminProvisioningService>();
        var email = Prompt("Administrator email: ");
        var password = PromptSecret("Password: ");
        var confirmation = PromptSecret("Confirm password: ");
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

    private static string Prompt(string label)
    {
        Console.Write(label);
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static string PromptSecret(string label)
    {
        Console.Write(label);
        var value = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return value.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (value.Length > 0)
                {
                    value.Length--;
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                value.Append(key.KeyChar);
            }
        }
    }
}