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
    public static void Write(string format, IEnumerable<ScreenplayDiagnostic> diagnostics) =>
        Write(format, diagnostics, null);

    /// <summary>
    /// Writes source provenance and diagnostics to standard error in the given output format.
    /// </summary>
    /// <param name="format">The resolved output format.</param>
    /// <param name="diagnostics">The diagnostics to write.</param>
    /// <param name="provenance">Optional source-provider and compatibility provenance.</param>
    public static void Write(
        string format,
        IEnumerable<ScreenplayDiagnostic> diagnostics,
        ScreenplayGenerationProvenance? provenance)
    {
        var materialized = diagnostics.ToArray();
        var groups = ScreenplayDiagnostics.GroupBySeverity(materialized);
        if (groups.Count == 0 && provenance is null)
        {
            return;
        }

        if (IsMachineReadable(format))
        {
            Console.Error.WriteLine(JsonFor(format, materialized, provenance));
            return;
        }

        WriteText(groups, provenance);
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

    /// <summary>
    /// Serializes provenance and diagnostics for a machine-readable output format.
    /// </summary>
    /// <param name="format">The resolved output format.</param>
    /// <param name="diagnostics">The diagnostics to serialize.</param>
    /// <param name="provenance">Optional source-provider and compatibility provenance.</param>
    /// <returns>The JSON payload.</returns>
    internal static string JsonFor(
        string format,
        IEnumerable<ScreenplayDiagnostic> diagnostics,
        ScreenplayGenerationProvenance? provenance)
    {
        var options = string.Equals(format, OutputFormats.Json, StringComparison.Ordinal)
            ? OutputFormatter.IndentedJsonSerializerOptions
            : OutputFormatter.JsonSerializerOptions;

        var payload = new
        {
            Provenance = provenance is null
                ? null
                : new
                {
                    provenance.Provider,
                    provenance.ProviderVersion,
                    Projects = provenance.Projects.Select(project => new
                    {
                        project.Project,
                        project.TargetFramework,
                        project.Packages,
                        project.Assemblies,
                        project.Capabilities
                    }),
                    Compatibility = provenance.Compatibility is null
                        ? null
                        : new
                        {
                            SupportTier = provenance.Compatibility.SupportTier.ToString(),
                            RecognitionStatus = provenance.Compatibility.RecognitionStatus.ToString(),
                            SemanticConformance = provenance.Compatibility.SemanticConformance.ToString(),
                            LoweringFidelity = provenance.Compatibility.LoweringFidelity.ToString(),
                            provenance.Compatibility.Explanation
                        }
                },
            Diagnostics = ScreenplayDiagnostics.GroupBySeverity(diagnostics)
                .SelectMany(group => group.Select(diagnostic => new
                {
                    Severity = LabelFor(diagnostic.Severity),
                    diagnostic.Code,
                    diagnostic.Message,
                    diagnostic.Location
                }))
        };

        return JsonSerializer.Serialize(payload, options);
    }

    internal static bool IsMachineReadable(string format) =>
        string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.JsonQuiet, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal);

    static void WriteText(
        IEnumerable<IGrouping<ScreenplayDiagnosticSeverity, ScreenplayDiagnostic>> groups,
        ScreenplayGenerationProvenance? provenance)
    {
        if (provenance is not null)
        {
            WriteProvenance(provenance);
        }

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

    static void WriteProvenance(ScreenplayGenerationProvenance provenance)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("source compatibility:");
        Console.Error.WriteLine($"  provider: {provenance.Provider} {provenance.ProviderVersion}");
        foreach (var project in provenance.Projects)
        {
            Console.Error.WriteLine($"  project: {project.Project} ({project.TargetFramework ?? "unknown target framework"})");
            Console.Error.WriteLine($"    packages: {Describe(project.Packages.Select(package => $"{package.Id} {package.Version}"))}");
            Console.Error.WriteLine($"    assemblies: {Describe(project.Assemblies.Select(assembly => $"{assembly.Name} {assembly.Version}"))}");
            Console.Error.WriteLine($"    capabilities: {Describe(project.Capabilities)}");
        }

        if (provenance.Compatibility is { } compatibility)
        {
            Console.Error.WriteLine($"  support tier: {compatibility.SupportTier}");
            Console.Error.WriteLine($"  recognition: {compatibility.RecognitionStatus}");
            Console.Error.WriteLine($"  semantic conformance: {compatibility.SemanticConformance}");
            Console.Error.WriteLine($"  lowering fidelity: {compatibility.LoweringFidelity}");
            Console.Error.WriteLine($"  evidence: {compatibility.Explanation}");
        }
    }

    static string Describe(IEnumerable<string> values)
    {
        var materialized = values.ToArray();
        return materialized.Length == 0 ? "none resolved" : string.Join(", ", materialized);
    }
}
