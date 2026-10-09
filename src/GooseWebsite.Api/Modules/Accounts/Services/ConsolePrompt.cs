using System.Text;

namespace GooseWebsite.Api.Modules.Accounts.Services;

// Reads operator input from the console. Secrets are never echoed, never taken from arguments.
internal static class ConsolePrompt
{
    public static string Text(string label)
    {
        Console.Write(label);
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    public static string Secret(string label)
    {
        Console.Write(label);
        var value = new StringBuilder();
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
