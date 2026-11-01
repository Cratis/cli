// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Reads a secret value from stdin when it is redirected and otherwise from a hidden prompt. Never from an argument.
/// </summary>
public static class SecretInput
{
    /// <summary>
    /// Reads a secret value.
    /// </summary>
    /// <returns>The value, without a trailing line break.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no value could be read.</exception>
    public static string Read() => Read(Console.IsInputRedirected, Console.In, () => ConsolePassphraseSource.Ask("Secret value", confirm: false));

    /// <summary>
    /// Reads a secret value from the given sources.
    /// </summary>
    /// <param name="inputRedirected">True when stdin carries the value.</param>
    /// <param name="input">The stdin reader.</param>
    /// <param name="prompt">Asks for the value on a terminal.</param>
    /// <returns>The value, without a trailing line break.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is empty.</exception>
    public static string Read(bool inputRedirected, TextReader input, Func<string> prompt)
    {
        var value = inputRedirected ? input.ReadToEnd().TrimEnd('\r', '\n') : prompt();
        return value.Length == 0 ? throw new InvalidOperationException("The secret value is empty.") : value;
    }
}
