// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Resolves the plain-text lines every generated code file starts with, from <c language="shell">--file-header</c>
/// or <c language="shell">--file-header-file</c>.
/// </summary>
/// <remarks>
/// Each <c language="shell">--file-header</c> value is one or more lines: a real line break or the two characters
/// <c language="text">\n</c> start a new line, so a shell can pass several lines in one value. A file contributes one line per line of
/// text, without its final line break. <c language="text">{year}</c> is replaced by the current UTC year. The renderer writes each line
/// as a comment in the file's language, so lines carry no comment markers. No lines means no header.
/// </remarks>
internal static class RenderFileHeader
{
    /// <summary>
    /// The placeholder replaced by the current UTC year.
    /// </summary>
    public const string YearPlaceholder = "{year}";

    /// <summary>
    /// The hint shown with a file header that cannot be used.
    /// </summary>
    public const string Hint = "Pass plain-text lines without comment markers with --file-header (repeat it, or separate lines with \\n), or name a text file with --file-header-file";

    /// <summary>
    /// Resolves the header lines from the command-line values or the header file.
    /// </summary>
    /// <param name="values">The <c language="shell">--file-header</c> values, in order.</param>
    /// <param name="file">The full path of the <c language="shell">--file-header-file</c>, or null.</param>
    /// <param name="year">The year that replaces <see cref="YearPlaceholder"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The header lines, empty for no header, or the reason the header cannot be used.</returns>
    public static async Task<(ImmutableArray<string> Lines, string? Error, bool NotFound)> Resolve(
        IReadOnlyList<string> values,
        string? file,
        int year,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return (Lines(values.SelectMany(SplitValue), year), null, false);
        }

        if (values.Count > 0)
        {
            return ([], "--file-header and --file-header-file are mutually exclusive", false);
        }

        string text;
        try
        {
            text = await File.ReadAllTextAsync(file, cancellationToken);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return ([], "The file header file was not found", true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return ([], $"The file header file cannot be read: {exception.Message}", false);
        }

        return (Lines(SplitText(text), year), null, false);
    }

    static ImmutableArray<string> Lines(IEnumerable<string> lines, int year) =>
        [.. lines.Select(line => line.Replace(YearPlaceholder, year.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))];

    static IEnumerable<string> SplitValue(string value) =>
        value.Replace("\\n", "\n", StringComparison.Ordinal).ReplaceLineEndings("\n").Split('\n');

    static string[] SplitText(string text)
    {
        var normalized = text.ReplaceLineEndings("\n");
        if (normalized.EndsWith('\n'))
        {
            normalized = normalized[..^1];
        }

        return normalized.Length == 0 ? [] : normalized.Split('\n');
    }
}
