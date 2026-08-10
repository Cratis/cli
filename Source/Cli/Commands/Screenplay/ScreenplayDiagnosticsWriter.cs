// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Renders generation diagnostics to standard error, grouped by severity.
/// </summary>
/// <remarks>
/// Diagnostics always go to standard error. The generated document may be on standard output, and mixing the two
/// would corrupt it — <c>cratis screenplay generate &gt; MyApp.play</c> has to keep working.
/// </remarks>
public static class ScreenplayDiagnosticsWriter
{
    /// <summary>
    /// Writes the diagnostics to standard error in the given output format.
    /// </summary>
    /// <param name="format">The resolved output format.</param>
    /// <param name="diagnostics">The diagnostics to write.</param>
    public static void Write(string format, IEnumerable<ScreenplayDiagnostic> diagnostics)
    {
        var groups = ScreenplayDiagnostics.GroupBySeverity(diagnostics);
        if (groups.Count == 0)
        {
            return;
        }

        if (IsMachineReadable(format))
        {
            WriteJson(format, groups);
            return;
        }

        WriteText(groups);
    }

    /// <summary>
    /// Gets the label used for a severity in text output.
    /// </summary>
    /// <param name="severity">The severity to label.</param>
    /// <returns>The label.</returns>
    public static string LabelFor(ScreenplayDiagnosticSeverity severity) => severity switch
    {
        ScreenplayDiagnosticSeverity.Error => "error",
        ScreenplayDiagnosticSeverity.Warning => "warning",
        _ => "info"
    };

    /// <summary>
    /// Gets the heading used for a group of diagnostics of the same severity.
    /// </summary>
    /// <param name="severity">The severity the group holds.</param>
    /// <returns>The heading.</returns>
    public static string GroupHeadingFor(ScreenplayDiagnosticSeverity severity) => severity switch
    {
        ScreenplayDiagnosticSeverity.Error => "errors",
        ScreenplayDiagnosticSeverity.Warning => "warnings",
        _ => "information"
    };

    /// <summary>
    /// Builds the line a single diagnostic is written as in text output.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to write.</param>
    /// <returns>The line.</returns>
    /// <remarks>
    /// The code and the location are both left out when they are absent — a diagnostic about a whole document has
    /// no location, and not every reporting system assigns codes.
    /// </remarks>
    public static string LineFor(ScreenplayDiagnostic diagnostic)
    {
        var code = string.IsNullOrWhiteSpace(diagnostic.Code) ? string.Empty : $" {diagnostic.Code}";
        var location = string.IsNullOrWhiteSpace(diagnostic.Location) ? string.Empty : $" [{diagnostic.Location}]";
        return $"  {LabelFor(diagnostic.Severity)}{code}:{location} {diagnostic.Message}";
    }

    static bool IsMachineReadable(string format) =>
        string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.JsonQuiet, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal);

    static void WriteJson(string format, IEnumerable<IGrouping<ScreenplayDiagnosticSeverity, ScreenplayDiagnostic>> groups)
    {
        var options = string.Equals(format, OutputFormats.Json, StringComparison.Ordinal)
            ? OutputFormatter.IndentedJsonSerializerOptions
            : OutputFormatter.JsonSerializerOptions;

        var payload = new
        {
            Diagnostics = groups.SelectMany(group => group.Select(diagnostic => new
            {
                Severity = LabelFor(diagnostic.Severity),
                diagnostic.Code,
                diagnostic.Message,
                diagnostic.Location
            }))
        };

        Console.Error.WriteLine(JsonSerializer.Serialize(payload, options));
    }

    static void WriteText(IEnumerable<IGrouping<ScreenplayDiagnosticSeverity, ScreenplayDiagnostic>> groups)
    {
        foreach (var group in groups)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"{GroupHeadingFor(group.Key)} ({group.Count()}):");

            foreach (var diagnostic in group)
            {
                Console.Error.WriteLine(LineFor(diagnostic));
            }
        }
    }
}
