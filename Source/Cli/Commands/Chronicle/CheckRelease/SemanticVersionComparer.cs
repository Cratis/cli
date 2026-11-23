// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.CheckRelease;

/// <summary>
/// Orders versions by Semantic Versioning 2.0 precedence: build metadata is ignored, a prerelease is older than its release,
/// numeric prerelease identifiers compare numerically, alphanumeric ones lexically, and numeric identifiers are older than alphanumeric ones.
/// </summary>
public static class SemanticVersionComparer
{
    /// <summary>
    /// Compares two versions that already passed <see cref="ReleaseReader.IsVersion"/>.
    /// </summary>
    /// <param name="left">The first version.</param>
    /// <param name="right">The second version.</param>
    /// <returns>Less than zero when <paramref name="left"/> is older, zero when equal, greater than zero when newer.</returns>
    public static int Compare(string left, string right)
    {
        var (leftCore, leftPrerelease) = Split(left);
        var (rightCore, rightPrerelease) = Split(right);
        for (var index = 0; index < 3; index++)
        {
            var part = CompareNumbers(leftCore[index], rightCore[index]);
            if (part != 0)
            {
                return part;
            }
        }

        if (leftPrerelease.Length == 0 || rightPrerelease.Length == 0)
        {
            return rightPrerelease.Length.CompareTo(leftPrerelease.Length);
        }

        return ComparePrerelease(leftPrerelease, rightPrerelease);
    }

    static int ComparePrerelease(string[] left, string[] right)
    {
        for (var index = 0; index < Math.Min(left.Length, right.Length); index++)
        {
            var part = CompareIdentifiers(left[index], right[index]);
            if (part != 0)
            {
                return part;
            }
        }

        return left.Length.CompareTo(right.Length);
    }

    static int CompareIdentifiers(string left, string right)
    {
        var leftNumeric = left.All(char.IsAsciiDigit);
        var rightNumeric = right.All(char.IsAsciiDigit);
        if (leftNumeric && rightNumeric)
        {
            return CompareNumbers(left, right);
        }

        if (leftNumeric != rightNumeric)
        {
            return leftNumeric ? -1 : 1;
        }

        return Math.Sign(string.CompareOrdinal(left, right));
    }

    static int CompareNumbers(string left, string right)
    {
        var a = left.TrimStart('0');
        var b = right.TrimStart('0');
        return a.Length != b.Length ? a.Length.CompareTo(b.Length) : Math.Sign(string.CompareOrdinal(a, b));
    }

    static (string[] Core, string[] Prerelease) Split(string version)
    {
        var text = version.TrimStart('v', 'V');
        var plus = text.IndexOf('+', StringComparison.Ordinal);
        text = plus >= 0 ? text[..plus] : text;
        var dash = text.IndexOf('-', StringComparison.Ordinal);
        var core = (dash >= 0 ? text[..dash] : text).Split('.');
        var prerelease = dash >= 0 ? text[(dash + 1)..].Split('.') : [];
        return (core, prerelease);
    }
}
