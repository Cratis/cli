// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.ReleaseNotes;

/// <summary>
/// Shows release notes in a terminal, one release at a time.
/// </summary>
public static class ReleaseNotesView
{
    /// <summary>
    /// Writes the notes of one release, or where to read them when this build does not include them.
    /// </summary>
    /// <param name="console">The console to write to.</param>
    /// <param name="note">The release.</param>
    public static void Write(IAnsiConsole console, ReleaseNote note)
    {
        console.Write(new Rule($"[bold]Cratis CLI {note.Version.EscapeMarkup()}[/]").LeftJustified().RuleStyle(new Style(OutputFormatter.Accent)));
        console.WriteLine();
        console.MarkupLine(note.Notes is null
            ? $"[{OutputFormatter.Muted.ToMarkup()}]The notes for this release are not included in this build. Read them at {note.Url.EscapeMarkup()}[/]"
            : ReleaseNotesMarkdown.ToMarkup(note.Notes));
        console.WriteLine();
    }

    /// <summary>
    /// Pages through releases, newest first, until the person quits.
    /// </summary>
    /// <param name="console">The console to write to.</param>
    /// <param name="notes">The releases, newest first.</param>
    /// <param name="readKey">Reads the next key press.</param>
    public static void Page(IAnsiConsole console, IReadOnlyList<ReleaseNote> notes, Func<ConsoleKey> readKey)
    {
        var navigator = new ReleaseNotesNavigator(notes);
        do
        {
            console.Clear();
            Write(console, navigator.Current);
            console.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]{Footer(navigator).EscapeMarkup()}[/]");
        }
        while (navigator.Handle(readKey()));
    }

    /// <summary>
    /// Describes where the person is and which keys move them.
    /// </summary>
    /// <param name="navigator">The navigator.</param>
    /// <returns>The footer text.</returns>
    internal static string Footer(ReleaseNotesNavigator navigator)
    {
        var keys = new List<string>();
        if (navigator.HasNewer)
        {
            keys.Add("\u2190/p newer");
        }

        if (navigator.HasOlder)
        {
            keys.Add("\u2192/n older");
        }

        keys.Add("q quit");
        return $"{navigator.Position} of {navigator.Count}  \u00b7  {string.Join("  ", keys)}";
    }
}
