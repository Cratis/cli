// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Affected;

/// <summary>
/// Matches repository relative paths against glob patterns. * stays within a segment, ** crosses segments,
/// and a pattern without a slash matches the file name at any depth.
/// </summary>
public static class Glob
{
    static readonly ConcurrentDictionary<(string Pattern, bool IgnoreCase), Regex> _cache = new();

    /// <summary>
    /// Gets whether a path matches a pattern.
    /// </summary>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="path">The repository relative path with forward slashes.</param>
    /// <param name="ignoreCase">Whether letter case is ignored.</param>
    /// <returns>True on a match.</returns>
    public static bool IsMatch(string pattern, string path, bool ignoreCase = false) =>
        _cache.GetOrAdd((pattern, ignoreCase), key => Compile(key.Pattern, key.IgnoreCase)).IsMatch(path);

    /// <summary>
    /// Gets whether a path matches any of the patterns.
    /// </summary>
    /// <param name="patterns">The glob patterns.</param>
    /// <param name="path">The repository relative path with forward slashes.</param>
    /// <param name="ignoreCase">Whether letter case is ignored.</param>
    /// <returns>True when at least one matches.</returns>
    public static bool IsMatchAny(IEnumerable<string> patterns, string path, bool ignoreCase = false) =>
        patterns.Any(pattern => IsMatch(pattern, path, ignoreCase));

    static Regex Compile(string pattern, bool ignoreCase)
    {
        var source = pattern.Replace('\\', '/');
        if (!source.Contains('/', StringComparison.Ordinal))
        {
            source = "**/" + source;
        }

        var builder = new StringBuilder("^");
        for (var index = 0; index < source.Length; index++)
        {
            var current = source[index];
            if (current == '*' && index + 1 < source.Length && source[index + 1] == '*')
            {
                index++;
                if (index + 1 < source.Length && source[index + 1] == '/')
                {
                    index++;
                    builder.Append("(?:.*/)?");
                }
                else
                {
                    builder.Append(".*");
                }
            }
            else if (current == '*')
            {
                builder.Append("[^/]*");
            }
            else if (current == '?')
            {
                builder.Append("[^/]");
            }
            else
            {
                builder.Append(Regex.Escape(current.ToString()));
            }
        }

        builder.Append('$');
        var options = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        return new Regex(builder.ToString(), options, TimeSpan.FromSeconds(1));
    }
}
