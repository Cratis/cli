// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.CritterStack.Screenplay;
using Cratis.Screenplay.Generation;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Generates Screenplay documents from Marten and Wolverine source code.
/// </summary>
public sealed class CritterStackScreenplayGeneration : IScreenplayGeneration
{
    /// <inheritdoc/>
    public async Task<GeneratedScreenplay> Generate(
        string targetPath,
        ScreenplayGenerationOptions options,
        CancellationToken cancellationToken) =>
        GenerateFrom(
            await ScreenplayCompilationLoader.Load(targetPath, includeAllProjects: true, options.TargetFramework, cancellationToken),
            targetPath,
            options);

    /// <summary>
    /// Generates from compilations that have already been loaded.
    /// </summary>
    /// <param name="loaded">The loaded project compilations.</param>
    /// <param name="targetPath">The solution or project path.</param>
    /// <param name="options">Generation options.</param>
    /// <returns>The generated Screenplay.</returns>
    internal static GeneratedScreenplay GenerateFrom(
        LoadedCompilation loaded,
        string targetPath,
        ScreenplayGenerationOptions options)
    {
        if (loaded.ProjectSourceAlignmentFailureResultFor(ScreenplayDiagnosticLocations.Target(targetPath)) is { } alignmentFailure)
        {
            return alignmentFailure;
        }

        if (!ScreenplaySourceStructureOptions.TryNormalize(options, out var normalizedOptions))
        {
            return new GeneratedScreenplay(
                string.Empty,
                [.. loaded.Diagnostics, ScreenplaySourceStructureOptions.InvalidFeatureRoot(targetPath)])
            {
                Projects = loaded.ProjectNames
            };
        }

        options = normalizedOptions;
        if (loaded.Compilations.Count == 0)
        {
            return new GeneratedScreenplay(string.Empty, loaded.Diagnostics);
        }

        var sourceErrors = SourceErrors(loaded);
        if (sourceErrors.Count > 0)
        {
            return new GeneratedScreenplay(string.Empty, [.. loaded.Diagnostics, .. sourceErrors])
            {
                Projects = loaded.ProjectNames
            };
        }

        var projects = ScreenplayProjectCompilations.From(loaded, targetPath);
        var optionDiagnostics = options.ModulesFromNamespaceRoots
            ?
            [
                new ScreenplayDiagnostic(
                    ScreenplayDiagnosticSeverity.Warning,
                    ScreenplayDiagnosticCodes.UnsupportedGenerationOption,
                    "The Marten and Critter Stack providers do not support --modules-from-namespace-roots; the option was not applied",
                    ScreenplayDiagnosticLocations.Target(targetPath))
            ]
            : Array.Empty<ScreenplayDiagnostic>();
        var result = new CritterStackScreenplayGenerator().Generate(
            projects,
            new CritterStackScreenplayOptions
            {
                Domain = options.Domain ?? DomainFrom(targetPath, loaded),
                FeatureRoot = options.FeatureRoot,
                Module = options.Module,
                NamespaceSegmentsToSkip = options.SegmentsToSkip ?? 0
            });

        return new GeneratedScreenplay(
            result.Source,
            [.. loaded.Diagnostics, .. optionDiagnostics, .. result.Diagnostics.Select(Map)])
        {
            Projects = loaded.ProjectNames
        };
    }

    internal static ScreenplayDiagnostic Map(GenerationDiagnostic diagnostic) => new(
        diagnostic.Severity switch
        {
            GenerationDiagnosticSeverity.Information => ScreenplayDiagnosticSeverity.Information,
            GenerationDiagnosticSeverity.Warning => ScreenplayDiagnosticSeverity.Warning,
            GenerationDiagnosticSeverity.Error => ScreenplayDiagnosticSeverity.Error,
            _ => ScreenplayDiagnosticSeverity.Error
        },
        diagnostic.Code,
        diagnostic.Message,
        diagnostic.Source?.Path)
    {
        Subject = diagnostic.Subject?.Value,
        Outcome = diagnostic.Outcome?.ToString()
    };

    static IReadOnlyList<ScreenplayDiagnostic> SourceErrors(LoadedCompilation loaded) =>
    [
        .. loaded.Compilations.SelectMany((compilation, index) => compilation.GetDiagnostics()
            .Where(_ => _.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Take(1)
            .Select(_ => new ScreenplayDiagnostic(
                ScreenplayDiagnosticSeverity.Error,
                ScreenplayDiagnosticCodes.SourceDidNotCompile,
                $"Source project '{loaded.ProjectNames[index]}' did not compile: {_.Id}",
                ScreenplayDiagnosticLocations.CompilationSource(loaded, index, _))))
    ];

    static string? DomainFrom(string targetPath, LoadedCompilation loaded) =>
        loaded.Compilations.Count > 1 ? Path.GetFileNameWithoutExtension(targetPath) : loaded.ProjectNames[0];
}
