// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Fills a branch template the way Studio does: every value written in lowercase with <c language="csharp">-</c> between words.
/// </summary>
public static class BuildBranchNames
{
    /// <summary>
    /// Fills a branch template.
    /// </summary>
    /// <param name="template">The template, such as <c language="csharp">studio/{feature}</c>.</param>
    /// <param name="values">The placeholder values.</param>
    /// <returns>The branch name.</returns>
    public static string From(string template, IReadOnlyDictionary<string, string> values) =>
        BuildKitPlaceholders.Fill(template, values.ToDictionary(pair => pair.Key, pair => Slug(pair.Value), StringComparer.Ordinal));

    /// <summary>
    /// Writes a value in lowercase with <c language="csharp">-</c> between words.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The slug.</returns>
    public static string Slug(string value)
    {
        var slug = new StringBuilder();
        var previous = ' ';
        foreach (var character in value.Normalize(NormalizationForm.FormKD))
        {
            var wordStarts = char.IsUpper(character) && (char.IsLower(previous) || char.IsDigit(previous));
            previous = character;
            if (char.IsAsciiLetterOrDigit(character))
            {
                if (wordStarts && slug.Length > 0 && slug[^1] != '-' && slug[^1] != '/') slug.Append('-');
                slug.Append(char.ToLowerInvariant(character));
            }
            else if (character == '/')
            {
                slug.Append('/');
            }
            else if (slug.Length > 0 && slug[^1] != '-' && slug[^1] != '/')
            {
                slug.Append('-');
            }
        }

        return slug.ToString().Trim('-');
    }
}
