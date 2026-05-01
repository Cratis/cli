// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli;

/// <summary>
/// Provides small animation utilities for the CLI banner and first-run experience.
/// </summary>
public static partial class BannerAnimations
{
    const int MaxTypewriterMs = 400;

    [GeneratedRegex(@"\[/?[^\]]*\]", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    static partial Regex StripMarkupRegex { get; }

    /// <summary>
    /// Renders a Spectre.Console markup string one visible character at a time.
    /// First renders the full styled line, then overwrites it character by character
    /// using direct console output for the typewriter reveal.
    /// Falls back to instant render when output is not a terminal.
    /// </summary>
    /// <param name="markup">The Spectre.Console markup string to render.</param>
    /// <param name="charDelayMs">Base delay in milliseconds between each visible character.</param>
    public static void TypewriterLine(string markup, int charDelayMs = 15)
    {
        if (!AnsiConsole.Profile.Out.IsTerminal)
        {
            AnsiConsole.MarkupLine(markup);
            return;
        }

        var plainText = StripMarkupRegex.Replace(markup, string.Empty);
        var visibleCount = plainText.Length;

        if (visibleCount == 0)
        {
            AnsiConsole.MarkupLine(markup);
            return;
        }

        // Cap total time to MaxTypewriterMs.
        var delay = Math.Min(charDelayMs, MaxTypewriterMs / Math.Max(visibleCount, 1));

        // Write characters one at a time using direct console output.
        // This avoids Spectre markup parsing issues with partial strings.
        Console.Write('\r');
        foreach (var ch in plainText)
        {
            Console.Write(ch);
            Thread.Sleep(delay);
        }

        // Overwrite with the properly styled markup version.
        Console.Write('\r');
        AnsiConsole.MarkupLine(markup);
    }

    /// <summary>
    /// A standardized brief pause between visual elements.
    /// </summary>
    public static void PauseBrief() => Thread.Sleep(50);
}
