// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>Expands comma-separated globs and their brace alternatives into concrete patterns.</summary>
static class AiGlobAlternatives
{
    /// <summary>Expands every top-level glob and every, possibly nested, brace group.</summary>
    /// <param name="applyTo">The comma-separated globs.</param>
    /// <param name="maximum">The largest number of patterns to produce.</param>
    /// <returns>The concrete patterns, or <see langword="null"/> when there are more than <paramref name="maximum"/> or the braces do not balance.</returns>
    internal static IReadOnlyList<string>? Expand(string applyTo, int maximum)
    {
        var patterns = new List<string>();
        foreach (var glob in SplitTopLevel(applyTo))
        {
            if (!TryExpand(glob.Trim(), maximum - patterns.Count, patterns)) return null;
        }

        return patterns;
    }

    static bool TryExpand(string glob, int remaining, List<string> patterns)
    {
        var open = glob.IndexOf('{');
        if (open < 0)
        {
            if (remaining <= 0) return false;
            patterns.Add(glob);
            return true;
        }

        var close = MatchingBrace(glob, open);
        if (close < 0) return false;

        var prefix = glob[..open];
        var suffix = glob[(close + 1)..];
        foreach (var alternative in SplitTopLevel(glob[(open + 1)..close]))
        {
            var before = patterns.Count;
            if (!TryExpand(prefix + alternative + suffix, remaining, patterns)) return false;
            remaining -= patterns.Count - before;
        }

        return true;
    }

    static int MatchingBrace(string glob, int open)
    {
        var depth = 0;
        for (var index = open; index < glob.Length; index++)
        {
            depth += BraceDelta(glob[index]);
            if (depth == 0) return index;
        }

        return -1;
    }

    static IEnumerable<string> SplitTopLevel(string value)
    {
        var depth = 0;
        var start = 0;
        for (var index = 0; index < value.Length; index++)
        {
            depth += BraceDelta(value[index]);
            if (value[index] != ',' || depth != 0) continue;

            yield return value[start..index];
            start = index + 1;
        }

        yield return value[start..];
    }

    static int BraceDelta(char character) => character switch
    {
        '{' => 1,
        '}' => -1,
        _ => 0
    };
}
