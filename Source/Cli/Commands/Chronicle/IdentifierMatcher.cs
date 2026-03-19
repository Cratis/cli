// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Provides partial/fuzzy matching for resource identifiers in CLI commands.
/// </summary>
public static class IdentifierMatcher
{
    /// <summary>
    /// Matches an input string against a collection of candidates using exact then partial (contains) matching.
    /// </summary>
    /// <typeparam name="T">The type of candidate items.</typeparam>
    /// <param name="candidates">The full collection of candidates.</param>
    /// <param name="input">The identifier input (exact or partial).</param>
    /// <param name="selector">Function to extract the identifier string from a candidate.</param>
    /// <param name="format">The output format (for error messages).</param>
    /// <param name="resourceLabel">Human-readable label for the resource type (e.g. "observer").</param>
    /// <returns>A tuple of (matched item or null, exit code).</returns>
    public static (T? Match, int ExitCode) Match<T>(
        IEnumerable<T> candidates,
        string input,
        Func<T, string> selector,
        string format,
        string resourceLabel = "resource")
        where T : class
    {
        var all = candidates.ToList();

        // 1. Exact match (case-insensitive)
        var exact = all.FirstOrDefault(c => string.Equals(selector(c), input, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return (exact, ExitCodes.Success);
        }

        // 2. Partial/contains match (case-insensitive)
        var partial = all.Where(c => selector(c).Contains(input, StringComparison.OrdinalIgnoreCase)).ToList();

        if (partial.Count == 1)
        {
            return (partial[0], ExitCodes.Success);
        }

        if (partial.Count > 1)
        {
            OutputFormatter.WriteError(
                format,
                $"Ambiguous {resourceLabel} identifier '{input}' — {partial.Count} matches found",
                $"Be more specific. Matching {resourceLabel}s:\n" + string.Join('\n', partial.Select(c => $"  {selector(c)}")),
                ExitCodes.ValidationErrorCode);
            return (null, ExitCodes.ValidationError);
        }

        OutputFormatter.WriteError(
            format,
            $"{resourceLabel} '{input}' not found",
            $"Use 'cratis chronicle {resourceLabel}s list' to see available {resourceLabel}s",
            ExitCodes.NotFoundCode);
        return (null, ExitCodes.NotFound);
    }
}
