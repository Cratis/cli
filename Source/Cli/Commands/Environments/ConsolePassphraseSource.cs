// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments.Secrets;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Asks for the passphrase on the terminal, hidden, and never stores it. Prompts go to stderr so output stays parseable.
/// </summary>
public class ConsolePassphraseSource : IPassphraseSource
{
    static readonly IAnsiConsole _console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) });

    /// <summary>
    /// Asks for a hidden value on the terminal.
    /// </summary>
    /// <param name="label">The prompt label.</param>
    /// <param name="confirm">True to ask twice and require both to match.</param>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no person can be prompted.</exception>
    public static string Ask(string label, bool confirm)
    {
        if (!GlobalSettings.IsInteractiveEnvironment())
        {
            throw new InvalidOperationException($"{label} is required but this is not an interactive terminal.");
        }

        while (true)
        {
            var first = _console.Prompt(new TextPrompt<string>($"{label}:").Secret());
            if (!confirm)
            {
                return first;
            }

            var second = _console.Prompt(new TextPrompt<string>($"Confirm {label.ToLowerInvariant()}:").Secret());
            if (string.Equals(first, second, StringComparison.Ordinal))
            {
                return first;
            }

            _console.MarkupLine("[red]The values did not match. Try again.[/]");
        }
    }

    /// <inheritdoc/>
    public string GetPassphrase() => Ask("Passphrase", confirm: false);

    /// <inheritdoc/>
    public string GetNewPassphrase() => Ask("New passphrase", confirm: true);
}
