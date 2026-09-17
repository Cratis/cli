// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

namespace Cratis.Templating.FileSystem;

/// <summary>
/// Glob matching compatible with template <c language="csharp">include</c>/<c language="csharp">exclude</c>/<c language="csharp">copyOnly</c> globs:
/// <c language="csharp">*</c> matches within a path segment, <c language="csharp">**</c> crosses directory boundaries, <c language="csharp">?</c> matches a
/// single character. Patterns are anchored at the source root; a pattern without a separator matches a
/// root-level file name. Both separators are accepted in patterns and paths.
/// </summary>
public static class GlobMatcher
{
    /// <summary>
    /// Determines whether a relative path matches a glob pattern.
    /// </summary>
    /// <param name="path">The relative path, using forward slashes.</param>
    /// <param name="glob">The glob pattern.</param>
    /// <returns>True when the path matches.</returns>
    public static bool Matches(string path, string glob)
    {
        path = path.Replace('\\', '/');
        glob = glob.Replace('\\', '/').TrimEnd('/');
        if (glob.Length == 0)
        {
            return false;
        }

        var anchored = glob.StartsWith('/');
        glob = anchored ? glob[1..] : glob;

        // A recursive prefix ("**/") matches the remainder at any depth, including the root.
        if (glob.StartsWith("**/") && glob.Length > 3)
        {
            var segments = path.Split('/');
            var remainder = glob[3..];
            return segments
                .Select((_, index) => string.Join('/', segments[index..]))
                .Any(suffix => Matches(suffix, remainder));
        }

        if (anchored || glob.Contains('/'))
        {
            return MatchSegments(path.Split('/'), glob.Split('/'));
        }

        // A bare file pattern matches a root-level file only.
        var fileName = Path.GetFileName(path);
        return path.IndexOf('/') < 0 && MatchSegment(fileName, glob);
    }

    /// <summary>
    /// Determines whether a path matches any of the globs.
    /// </summary>
    /// <param name="path">The relative path.</param>
    /// <param name="globs">The glob patterns.</param>
    /// <returns>True when any pattern matches.</returns>
    public static bool MatchesAny(string path, IEnumerable<string> globs) =>
        globs.Any(glob => Matches(path, glob));

    static bool MatchSegments(string[] pathSegments, string[] patternSegments)
    {
        return Match(pathSegments, 0, patternSegments, 0);
    }

    static bool Match(string[] pathSegments, int pathIndex, string[] patternSegments, int patternIndex)
    {
        while (true)
        {
            if (patternIndex == patternSegments.Length)
            {
                return pathIndex == pathSegments.Length;
            }

            if (patternSegments[patternIndex] == "**")
            {
                // "**" consumes zero or more path segments.
                for (var skip = pathIndex; skip <= pathSegments.Length; skip++)
                {
                    if (Match(pathSegments, skip, patternSegments, patternIndex + 1))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (pathIndex == pathSegments.Length)
            {
                return false;
            }

            if (!MatchSegment(pathSegments[pathIndex], patternSegments[patternIndex]))
            {
                return false;
            }

            pathIndex++;
            patternIndex++;
        }
    }

    static bool MatchSegment(string segment, string pattern)
    {
        var table = new bool[segment.Length + 1][];
        for (var i = 0; i <= segment.Length; i++)
        {
            table[i] = new bool[pattern.Length + 1];
        }
        table[0][0] = true;

        for (var patternIndex = 1; patternIndex <= pattern.Length; patternIndex++)
        {
            table[0][patternIndex] = pattern[patternIndex - 1] == '*' && table[0][patternIndex - 1];
        }

        for (var segmentIndex = 1; segmentIndex <= segment.Length; segmentIndex++)
        {
            for (var patternIndex = 1; patternIndex <= pattern.Length; patternIndex++)
            {
                var patternChar = pattern[patternIndex - 1];
                table[segmentIndex][patternIndex] = patternChar switch
                {
                    '*' => table[segmentIndex][patternIndex - 1] || table[segmentIndex - 1][patternIndex],
                    '?' => table[segmentIndex - 1][patternIndex - 1],
                    _ => (char.ToLowerInvariant(patternChar) == char.ToLowerInvariant(segment[segmentIndex - 1])
                          || char.ToUpperInvariant(patternChar) == segment[segmentIndex - 1])
                         && table[segmentIndex - 1][patternIndex - 1]
                };
            }
        }

        return table[segment.Length][pattern.Length];
    }
}
