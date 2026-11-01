// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.ReleaseNotes;

/// <summary>
/// Turns the markdown of release notes into terminal markup: headings, bullets, code, bold text and links.
/// Anything else is shown as written.
/// </summary>
public static partial class ReleaseNotesMarkdown
{
    [GeneratedRegex(@"^(?<level>#{1,6})\s+(?<text>.*)$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Heading { get; }

    [GeneratedRegex(@"^(?<indent>\s*)[-*+]\s+(?<text>.*)$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Bullet { get; }

    [GeneratedRegex(@"`(?<code>[^`]+)`|\*\*(?<bold>[^*]+)\*\*|\[(?<label>[^\]]+)\]\((?<url>[^)\s]+)\)", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex InlineElement { get; }

    /// <summary>
    /// Converts release notes markdown to Spectre.Console markup.
    /// </summary>
    /// <param name="markdown">The markdown.</param>
    /// <returns>The markup, with every literal bracket escaped.</returns>
    public static string ToMarkup(string markdown)
    {
        var accent = OutputFormatter.Accent.ToMarkup();
        var muted = OutputFormatter.Muted.ToMarkup();
        var builder = new StringBuilder();
        var inCode = false;

        foreach (var line in markdown.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inCode = !inCode;
                continue;
            }

            builder.AppendLine(inCode ? $"    [{muted}]{line.EscapeMarkup()}[/]" : Line(line, accent, muted));
        }

        return builder.ToString().TrimEnd();
    }

    static string Line(string line, string accent, string muted)
    {
        var heading = Heading.Match(line);
        if (heading.Success)
        {
            var style = heading.Groups["level"].Length <= 2 ? $"bold {accent}" : "bold";
            return $"[{style}]{Inline(heading.Groups["text"].Value, accent, muted)}[/]";
        }

        var bullet = Bullet.Match(line);
        if (bullet.Success)
        {
            return $"{bullet.Groups["indent"].Value}  [{accent}]\u2022[/] {Inline(bullet.Groups["text"].Value, accent, muted)}";
        }

        return Inline(line, accent, muted);
    }

    static string Inline(string text, string accent, string muted)
    {
        var builder = new StringBuilder();
        var position = 0;
        foreach (Match match in InlineElement.Matches(text))
        {
            builder.Append(text[position..match.Index].EscapeMarkup()).Append(InlineElementMarkup(match, accent, muted));
            position = match.Index + match.Length;
        }

        builder.Append(text[position..].EscapeMarkup());
        return builder.ToString();
    }

    static string InlineElementMarkup(Match match, string accent, string muted)
    {
        if (match.Groups["code"].Success)
        {
            return $"[{accent}]{match.Groups["code"].Value.EscapeMarkup()}[/]";
        }

        if (match.Groups["bold"].Success)
        {
            return $"[bold]{match.Groups["bold"].Value.EscapeMarkup()}[/]";
        }

        return $"{match.Groups["label"].Value.EscapeMarkup()} [{muted}]({match.Groups["url"].Value.EscapeMarkup()})[/]";
    }
}
