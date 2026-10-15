// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Writes grouped runtime diagnostics for both readiness and refusals.
/// </summary>
internal static class StageDiagnosticOutput
{
    internal static void WriteNotes(string format, IReadOnlyList<StageRuntimeNote>? notes, bool verbose)
    {
        if (notes is not { Count: > 0 }) return;
        if (verbose || string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal))
        {
            WriteGroups(format, StageRefusalGroups.Create([], notes));
            return;
        }

        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]{notes.Count} notes the engine ignored (use --verbose to list them)[/]");
    }

    internal static void WriteGroups(string format, IReadOnlyList<StageRefusalGroup> groups)
    {
        foreach (var category in groups.GroupBy(group => group.Category))
        {
            WriteLine(format, $"{category.Key} ({category.Sum(group => group.Count)})", heading: true);
            foreach (var group in category)
            {
                WriteLine(format, group.Summary);
            }
        }
    }

    internal static void WriteLine(string format, string text, bool heading = false)
    {
        if (string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal))
        {
            Console.WriteLine(text);
            return;
        }

        AnsiConsole.MarkupLine(heading ? $"  [bold]{text.EscapeMarkup()}[/]" : $"    {text.EscapeMarkup()}");
    }
}
