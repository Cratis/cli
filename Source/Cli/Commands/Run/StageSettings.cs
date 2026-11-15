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
        var effective = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var separator = entry.IndexOf('=', StringComparison.Ordinal);
            var name = $"Stage__{entry[..separator].Replace(":", "__", StringComparison.Ordinal)}";
            effective.Remove(name);
            effective[name] = entry[(separator + 1)..];
        }

        return [.. effective.SelectMany(pair => new[] { "-e", $"{pair.Key}={pair.Value}" })];
    }

    /// <summary>
    /// Appends explicit settings to generated arguments, dropping any generated environment entry they override.
    /// </summary>
    /// <param name="generated">The arguments the CLI generated, in order.</param>
    /// <param name="settings">The explicit Stage settings.</param>
    /// <returns>The generated arguments without overridden entries, followed by the explicit ones.</returns>
    public static IReadOnlyList<string> Override(IEnumerable<string> generated, IReadOnlyList<string>? settings)
    {
        var explicitArguments = BuildArguments(settings ?? []);
        var names = explicitArguments.Where((_, index) => index % 2 == 1)
            .Select(argument => argument[..argument.IndexOf('=', StringComparison.Ordinal)])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var source = generated.ToArray();
        var result = new List<string>();
        for (var index = 0; index < source.Length; index++)
        {
            var overridden = source[index] == "-e" && index + 1 < source.Length
                && source[index + 1].IndexOf('=', StringComparison.Ordinal) is > 0 and var separator
                && names.Contains(source[index + 1][..separator]);
            if (overridden) index++;
            else result.Add(source[index]);
        }

        result.AddRange(explicitArguments);
        return result;
    }

    static bool IsValid(string setting)
    {
        var separator = setting.IndexOf('=', StringComparison.Ordinal);
        return separator > 0 && KeyPattern.IsMatch(setting[..separator]) && !setting.Any(character => character is '\n' or '\r' or '\0');
    }
}
