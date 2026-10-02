// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Renders what the view command reports - what is shown, where it came from and where it is served.
/// </summary>
public static class ViewOutput
{
    /// <summary>
    /// The number of diagnostics of each severity listed before the rest are only counted.
    /// </summary>
    public const int DiagnosticsShown = 10;

    const int LabelWidth = 14;

    /// <summary>
    /// Writes what is about to be viewed.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="projectFile">The project being viewed.</param>
    /// <param name="output">What the project builds.</param>
    public static void WriteHeader(string format, string projectFile, ProjectOutput output)
    {
        if (!IsTable(format))
        {
            return;
        }

        AnsiConsole.WriteLine();
        OutputFormatter.WriteLabel("Project", projectFile, LabelWidth);
        OutputFormatter.WriteLabel("Assembly", output.IsBuilt ? output.TargetPath : $"{output.TargetPath} (not built)", LabelWidth);
    }

    /// <summary>
    /// Writes what finding the documents reported.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="diagnostics">The diagnostics to write.</param>
    public static void WriteDiagnostics(string format, IReadOnlyList<ViewDiagnostic> diagnostics)
    {
        if (!IsTable(format) || diagnostics.Count == 0)
        {
            return;
        }

        AnsiConsole.WriteLine();
        Write([.. diagnostics.Where(_ => _.IsError)], "error", OutputFormatter.Danger);
        Write([.. diagnostics.Where(_ => !_.IsError)], "warning", OutputFormatter.Warning);
    }

    /// <summary>
    /// Writes where the viewer is served and what it shows.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="address">The address the viewer is served at.</param>
    /// <param name="documents">Where the documents came from.</param>
    /// <param name="catalog">The documents being shown.</param>
    public static void WriteServing(string format, Uri address, ViewDocuments documents, EventModelCatalog catalog)
    {
        var count = catalog.Projects.Sum(_ => _.Documents.Count);
        var origin = documents.Origin == ViewOrigin.Embedded ? "embedded" : "generated from source";

        if (!IsTable(format))
        {
            OutputFormatter.WriteObject(format, new
            {
                url = address.ToString(),
                origin = documents.Origin.ToString().ToLowerInvariant(),
                projects = catalog.Projects.Select(_ => _.Name).ToArray(),
                documents = count
            });
            return;
        }

        var muted = OutputFormatter.Muted.ToMarkup();
        OutputFormatter.WriteLabel("Documents", $"{count} {origin} ({string.Join(", ", catalog.Projects.Select(_ => _.Name))})", LabelWidth);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [bold]Event model viewer[/] [link={address}]{address.ToString().EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"  [{muted}]Press Ctrl+C to stop.[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Writes that a browser could not be opened.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="address">The address to open by hand.</param>
    public static void WriteBrowserNotOpened(string format, Uri address)
    {
        if (IsTable(format))
        {
            AnsiConsole.MarkupLine($"  [{OutputFormatter.Warning.ToMarkup()}]Could not open a browser - open {address.ToString().EscapeMarkup()} yourself.[/]");
        }
    }

    static void Write(List<ViewDiagnostic> diagnostics, string label, Color color)
    {
        foreach (var diagnostic in diagnostics.Take(DiagnosticsShown))
        {
            var location = string.IsNullOrWhiteSpace(diagnostic.Location) ? string.Empty : $"{diagnostic.Location}: ";
            AnsiConsole.MarkupLine($"  [{color.ToMarkup()}]{label}[/] {diagnostic.Code.EscapeMarkup()}: {location.EscapeMarkup()}{diagnostic.Message.EscapeMarkup()}");
        }

        if (diagnostics.Count > DiagnosticsShown)
        {
            AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]... and {diagnostics.Count - DiagnosticsShown} more {label}s[/]");
        }
    }

    static bool IsTable(string format) => string.Equals(format, OutputFormats.Table, StringComparison.Ordinal);
}
