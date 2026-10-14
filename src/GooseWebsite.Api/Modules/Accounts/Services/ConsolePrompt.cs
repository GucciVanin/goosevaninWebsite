using System.Text;

namespace GooseWebsite.Api.Modules.Accounts.Services;

// Reads operator input. At a terminal, secrets are masked as they are typed. When input is piped in (a
// script), answers are read line by line from standard input. Secrets are never taken from arguments.
internal sealed class ConsolePrompt(TextReader? input = null)
{
    private readonly TextReader? _pipedInput = input ?? (Console.IsInputRedirected ? Console.In : null);

    // Windows PowerShell 5.1 puts a UTF-8 byte-order mark in front of text piped to a program; it is not part of the answer.
    private string? ReadLine() => (_pipedInput is null ? Console.ReadLine() : _pipedInput.ReadLine())?.TrimStart('\uFEFF');

    public string Text(string label)
    {
        Console.Write(label);
        return ReadLine()?.Trim() ?? string.Empty;
    }

    public string Secret(string label)
    {
        Console.Write(label);
        if (_pipedInput is not null)
        {
            Console.WriteLine();
            return ReadLine()?.TrimEnd('\r', '\n') ?? string.Empty;
        }

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
