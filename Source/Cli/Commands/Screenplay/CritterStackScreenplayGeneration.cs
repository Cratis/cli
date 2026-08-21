// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.CritterStack.Screenplay;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

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
            await ScreenplayCompilationLoader.Load(targetPath, includeAllProjects: true, cancellationToken),
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

        var sourceRoot = Path.GetDirectoryName(targetPath);
        var projects = loaded.Compilations
            .Select((compilation, index) => new DotNetProjectCompilation
            {
                Name = loaded.ProjectNames[index],
                ProjectPath = targetPath,
                SourceRoot = sourceRoot,
                Compilation = compilation
            })
            .ToArray();
        var result = new CritterStackScreenplayGenerator().Generate(
            projects,
            new CritterStackScreenplayOptions
            {
                Domain = options.Domain ?? DomainFrom(targetPath, loaded),
                Module = options.Module,
                NamespaceSegmentsToSkip = options.SegmentsToSkip ?? 0
            });

        return new GeneratedScreenplay(
            result.Source,
            [.. loaded.Diagnostics, .. result.Diagnostics.Select(Map)])
        {
            Projects = loaded.ProjectNames
        };
    }

    static IReadOnlyList<ScreenplayDiagnostic> SourceErrors(LoadedCompilation loaded) =>
    [
        .. loaded.Compilations.SelectMany((compilation, index) => compilation.GetDiagnostics()
            .Where(_ => _.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Take(1)
            .Select(_ => new ScreenplayDiagnostic(
                ScreenplayDiagnosticSeverity.Error,
                ScreenplayDiagnosticCodes.SourceDidNotCompile,
                $"Source project '{loaded.ProjectNames[index]}' did not compile: {_.Id} {_.GetMessage()}",
                _.Location.GetLineSpan().Path)))
    ];

    static string? DomainFrom(string targetPath, LoadedCompilation loaded) =>
        loaded.Compilations.Count > 1 ? Path.GetFileNameWithoutExtension(targetPath) : loaded.ProjectNames[0];

    static ScreenplayDiagnostic Map(GenerationDiagnostic diagnostic) => new(
        diagnostic.Severity switch
        {
            GenerationDiagnosticSeverity.Information => ScreenplayDiagnosticSeverity.Information,
            GenerationDiagnosticSeverity.Warning => ScreenplayDiagnosticSeverity.Warning,
            GenerationDiagnosticSeverity.Error => ScreenplayDiagnosticSeverity.Error,
            _ => ScreenplayDiagnosticSeverity.Error
        },
        diagnostic.Code,
        diagnostic.Message,
        diagnostic.Source?.Path);
}
