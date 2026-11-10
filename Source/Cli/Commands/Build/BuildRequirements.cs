// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Checks the commands a kit requires, each with an optional minimum version such as <c language="csharp">&gt;=10</c>.
/// </summary>
public static partial class BuildRequirements
{
    [GeneratedRegex(@"(?<major>\d+)(\.(?<minor>\d+))?(\.(?<patch>\d+))?", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex VersionNumber { get; }

    [GeneratedRegex(@"^(?<operator>>=|<=|>|<|=)?(?<version>\d+(\.\d+){0,2})$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Constraint { get; }

    /// <summary>
    /// Finds every requirement the machine does not meet.
    /// </summary>
    /// <param name="kit">The kit.</param>
    /// <param name="workingDirectory">Where to run the checks.</param>
    /// <param name="cancellationToken">Cancels the checks.</param>
    /// <returns>Each unmet requirement, said for people.</returns>
    public static async Task<IReadOnlyList<string>> Unmet(BuildKit kit, string workingDirectory, CancellationToken cancellationToken)
    {
        var unmet = new List<string>();
        foreach (var (command, version) in kit.Requires)
        {
            var result = await BuildProcess.Run(command, ["--version"], workingDirectory, cancellationToken: cancellationToken);
            if (result.ExitCode != 0)
            {
                unmet.Add($"{command} is not installed.");
                continue;
            }

            var installed = VersionNumber.Match($"{result.Output} {result.Error}");
            if (version is not null && installed.Success && !Satisfies(Parse(installed), version))
            {
                unmet.Add($"{command} {installed.Value} is installed; the kit needs {version}.");
            }
        }

        return unmet;
    }

    /// <summary>
    /// Gets a value indicating whether a version meets a constraint such as <c language="csharp">&gt;=10</c>.
    /// </summary>
    /// <param name="installed">The installed version.</param>
    /// <param name="constraint">The constraint, one or two space-separated comparisons.</param>
    /// <returns><see langword="true"/> when it does, or when the constraint cannot be read.</returns>
    public static bool Satisfies(System.Version installed, string constraint) =>
        constraint.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(part =>
        {
            var match = Constraint.Match(part);
            if (!match.Success) return true;
            var required = System.Version.Parse(match.Groups["version"].Value.Contains('.', StringComparison.Ordinal) ? match.Groups["version"].Value : $"{match.Groups["version"].Value}.0");
            var comparison = Compare(installed, required, match.Groups["version"].Value.Count(character => character == '.') + 1);
            return match.Groups["operator"].Value switch
            {
                ">" => comparison > 0,
                "<" => comparison < 0,
                "<=" => comparison <= 0,
                "=" => comparison == 0,
                _ => comparison >= 0
            };
        });

    /// <summary>
    /// Compares only as many parts as the constraint names, so <c language="csharp">&gt;=10</c> accepts 10.0.100.
    /// </summary>
    /// <param name="installed">The installed version.</param>
    /// <param name="required">The version the constraint names.</param>
    /// <param name="parts">How many parts the constraint names.</param>
    /// <returns>Less than, equal to or greater than zero.</returns>
    static int Compare(System.Version installed, System.Version required, int parts)
    {
        var mine = new[] { installed.Major, Math.Max(0, installed.Minor), Math.Max(0, installed.Build) };
        var theirs = new[] { required.Major, Math.Max(0, required.Minor), Math.Max(0, required.Build) };
        for (var index = 0; index < parts; index++)
        {
            if (mine[index] != theirs[index]) return mine[index].CompareTo(theirs[index]);
        }

        return 0;
    }

    static System.Version Parse(Match match) => new(
        int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture),
        match.Groups["minor"].Success ? int.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture) : 0,
        match.Groups["patch"].Success ? int.Parse(match.Groups["patch"].Value, CultureInfo.InvariantCulture) : 0);
}
