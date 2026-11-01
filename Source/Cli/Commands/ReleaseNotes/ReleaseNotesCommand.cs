// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Version;

namespace Cratis.Cli.Commands.ReleaseNotes;

/// <summary>
/// Shows the release notes of the CLI, which every build carries for every release up to its own.
/// </summary>
[LlmDescription("Shows the release notes of the cratis CLI. Interactive terminals page through every release, newest first; otherwise the latest release is printed. Use --version for one release, --since for every release after a version up to the running one, and -o json for all of them as data.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("release-notes", "Show what changed in each release of the Cratis CLI")]
[CliExample("release-notes")]
[CliExample("release-notes", "--version", "3.86.0")]
[CliExample("release-notes", "--since", "3.80.0")]
[CliExample("release-notes", "-o", "json")]
[LlmOutputAdvice("json", "JSON is an array of { version, notes, url }, newest first; notes is markdown, or null when this build does not include them, in which case url has them.")]
[LlmOption("--version", "string", "Show the notes of this release only")]
[LlmOption("--since", "string", "Show the releases after this version, up to the running one")]
public class ReleaseNotesCommand : Command<ReleaseNotesSettings>
{
    /// <inheritdoc/>
    public override int Execute(CommandContext context, ReleaseNotesSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var history = ReleaseNoteHistory.Embedded();
        var selected = Select(history, VersionCommand.GetCliVersion(), settings);

        if (selected is null)
        {
            OutputFormatter.WriteError(format, $"No release notes for version '{settings.Version}'", "Run 'cratis release-notes -q' to list the releases this build knows", ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var (presentation, notes) = Plan(format, GlobalSettings.IsInteractiveEnvironment(), settings, selected);
        Present(presentation, notes, format);
        return ExitCodes.Success;
    }

    /// <summary>
    /// Selects the releases asked for.
    /// </summary>
    /// <param name="history">Every release this build knows.</param>
    /// <param name="currentVersion">The version of the running CLI.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The releases, newest first, or null when the one release asked for is unknown.</returns>
    internal static IReadOnlyList<ReleaseNote>? Select(ReleaseNoteHistory history, string currentVersion, ReleaseNotesSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.Version))
        {
            return history.Find(settings.Version) is { } note ? [note] : null;
        }

        return string.IsNullOrWhiteSpace(settings.Since) ? history.Notes : history.Between(settings.Since, currentVersion);
    }

    /// <summary>
    /// Decides how to present the selected releases, and which of them.
    /// </summary>
    /// <param name="format">The resolved output format.</param>
    /// <param name="interactive">Whether a person can be asked and can press keys.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="selected">The selected releases, newest first.</param>
    /// <returns>The presentation and the releases to present.</returns>
    /// <remarks>
    /// Without a person to page, the whole history would be a wall of text, so only the latest release is written;
    /// <c language="csharp">--since</c> writes everything it selected, and JSON always carries everything.
    /// </remarks>
    internal static (ReleaseNotesPresentation Presentation, IReadOnlyList<ReleaseNote> Notes) Plan(string format, bool interactive, ReleaseNotesSettings settings, IReadOnlyList<ReleaseNote> selected)
    {
        if (string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) || string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal) || string.Equals(format, OutputFormats.JsonQuiet, StringComparison.Ordinal))
        {
            return (ReleaseNotesPresentation.Json, selected);
        }

        if (selected.Count == 0)
        {
            return (ReleaseNotesPresentation.Nothing, selected);
        }

        if (format == OutputFormats.Quiet)
        {
            return (ReleaseNotesPresentation.Versions, selected);
        }

        var all = selected.Count == 1 || !string.IsNullOrWhiteSpace(settings.Since);
        var printed = all ? selected : [selected[0]];
        if (format != OutputFormats.Table || !interactive)
        {
            return (format == OutputFormats.Table ? ReleaseNotesPresentation.Formatted : ReleaseNotesPresentation.Markdown, printed);
        }

        if (selected.Count == 1)
        {
            return (ReleaseNotesPresentation.Formatted, selected);
        }

        var asks = !string.IsNullOrWhiteSpace(settings.Since) && !settings.Yes;
        return (asks ? ReleaseNotesPresentation.AskThenPager : ReleaseNotesPresentation.Pager, selected);
    }

    /// <summary>
    /// Shapes releases for JSON output.
    /// </summary>
    /// <param name="notes">The releases.</param>
    /// <returns>One object per release with its version, notes and release page.</returns>
    internal static object[] ToJson(IReadOnlyList<ReleaseNote> notes) => [.. notes.Select(_ => (object)new { _.Version, _.Notes, _.Url })];

    static void Present(ReleaseNotesPresentation presentation, IReadOnlyList<ReleaseNote> notes, string format)
    {
        switch (presentation)
        {
            case ReleaseNotesPresentation.Json:
                OutputFormatter.WriteObject(format, ToJson(notes));
                break;
            case ReleaseNotesPresentation.Versions:
                notes.ToList().ForEach(_ => Console.WriteLine(_.Version));
                break;
            case ReleaseNotesPresentation.Markdown:
                notes.ToList().ForEach(_ => Console.WriteLine($"# {_.Version}{Environment.NewLine}{Environment.NewLine}{_.Notes ?? _.Url}{Environment.NewLine}"));
                break;
            case ReleaseNotesPresentation.Formatted:
                notes.ToList().ForEach(_ => ReleaseNotesView.Write(AnsiConsole.Console, _));
                break;
            case ReleaseNotesPresentation.AskThenPager when !AnsiConsole.Confirm($"{notes.Count} releases came out since your previous version. Show what changed?"):
                break;
            case ReleaseNotesPresentation.Pager or ReleaseNotesPresentation.AskThenPager:
                ReleaseNotesView.Page(AnsiConsole.Console, notes, () => Console.ReadKey(true).Key);
                break;
        }
    }
}
