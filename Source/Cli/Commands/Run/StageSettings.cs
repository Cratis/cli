// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Validates explicit Stage configuration and hands it to Docker as environment arguments, never shell text.
/// </summary>
public static partial class StageSettings
{
    [GeneratedRegex(@"\A[A-Za-z][A-Za-z0-9]*(?::[A-Za-z0-9]+)*\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex KeyPattern { get; }

    /// <summary>
    /// Checks settings whose keys are relative to the Stage configuration section.
    /// </summary>
    /// <param name="settings">Colon-separated keys and values, each joined by the first equals sign.</param>
    /// <returns>An error without setting values, or null when all settings are valid.</returns>
    public static string? Error(IEnumerable<string> settings) => settings.All(IsValid)
        ? null
        : "--stage-setting requires KEY=VALUE with a colon-separated alphanumeric key relative to Stage (for example Sandbox:Users=true). Keys must start with a letter; values cannot contain newlines or null characters.";

    /// <summary>
    /// Builds environment arguments, preserving each value as part of one argv entry.
    /// </summary>
    /// <param name="settings">The explicit Stage settings.</param>
    /// <returns>The Docker arguments.</returns>
    /// <exception cref="ArgumentException">Thrown when a setting is invalid.</exception>
    public static IReadOnlyList<string> BuildArguments(IEnumerable<string> settings)
    {
        var entries = settings.ToArray();
        if (Error(entries) is { } error) throw new ArgumentException(error, nameof(settings));
        return [.. entries.SelectMany(entry =>
        {
            var separator = entry.IndexOf('=', StringComparison.Ordinal);
            return new[] { "-e", $"Stage__{entry[..separator].Replace(":", "__", StringComparison.Ordinal)}={entry[(separator + 1)..]}" };
        })];
    }

    static bool IsValid(string setting)
    {
        var separator = setting.IndexOf('=', StringComparison.Ordinal);
        return separator > 0 && KeyPattern.IsMatch(setting[..separator]) && !setting.Any(character => character is '\n' or '\r' or '\0');
    }
}
