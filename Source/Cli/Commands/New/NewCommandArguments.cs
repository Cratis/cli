// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Cli.Commands.New;

/// <summary>
/// Partitions the command line for the <c language="csharp">new</c> command into what the CLI framework parses and
/// what belongs to the template. The framework silently discards options it does not know, which
/// would swallow template parameters such as <c language="csharp">--Framework net10.0</c> before the binder ever sees
/// them — a silent drop of exactly the kind this command must never do. Unknown options are
/// therefore captured here, before parsing, and handed to the template parameter binder instead.
/// </summary>
public static class NewCommandArguments
{
    static readonly string[] _knownOptions = ExtractKnownOptions();
    static readonly string[] _reservedOptions = ["-h", "--help"];

    /// <summary>
    /// Gets the tokens captured as template arguments by the last <see cref="Partition"/> call.
    /// </summary>
    public static IReadOnlyList<string> Captured { get; private set; } = [];

    /// <summary>
    /// Partitions arguments for the new command: known and reserved options are forwarded to the
    /// CLI framework, every other option token — plus the non-option token following it as its
    /// value — is captured for the template parameter binder.
    /// </summary>
    /// <param name="args">The raw command line arguments.</param>
    /// <returns>The arguments to forward to the CLI framework.</returns>
    public static string[] Partition(IReadOnlyList<string> args)
    {
        var forwarded = new List<string>();
        var captured = new List<string>();
        for (var index = 0; index < args.Count; index++)
        {
            var token = args[index];
            if (IsOption(token) && !IsKnown(token))
            {
                captured.Add(token);

                // A value form without '=' consumes the next non-option token as its value, the
                // same association the binder applies — boolean flags stand alone.
                if (!token.Contains('=', StringComparison.Ordinal)
                    && index + 1 < args.Count
                    && !IsOption(args[index + 1]))
                {
                    captured.Add(args[++index]);
                }
                continue;
            }

            forwarded.Add(token);
        }

        Captured = captured;
        return [.. forwarded];
    }

    static bool IsOption(string token) => token.StartsWith('-') && token.Length > 1;

    static bool IsKnown(string token)
    {
        var name = token.Contains('=', StringComparison.Ordinal) ? token[..token.IndexOf('=')] : token;
        return _knownOptions.Contains(name, StringComparer.OrdinalIgnoreCase)
            || _reservedOptions.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    static string[] ExtractKnownOptions() =>
    [
        .. typeof(NewSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(property => property.GetCustomAttributes<CommandOptionAttribute>())
            .SelectMany(attribute => attribute.LongNames.Select(name => "--" + name)
                .Concat(attribute.ShortNames.Select(name => "-" + name)))
    ];
}
