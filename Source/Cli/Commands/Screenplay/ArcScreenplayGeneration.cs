// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Generates a Screenplay document by loading the target into Roslyn compilations and handing them to the
/// <c>Cratis.Arc.Screenplay</c> generator.
/// </summary>
/// <remarks>
/// This is the only place in the CLI that knows the generator exists. Everything else is expressed against
/// <see cref="IScreenplayGeneration"/>.
/// </remarks>
public sealed class ArcScreenplayGeneration : IScreenplayGeneration
{
    /// <inheritdoc/>
    public async Task<GeneratedScreenplay> Generate(string targetPath, ScreenplayGenerationOptions options, CancellationToken cancellationToken) =>
        GenerateFrom(await ScreenplayCompilationLoader.Load(targetPath, options.TargetFramework, cancellationToken), targetPath, options);

    /// <summary>
    /// Generates the document from compilations that have already been loaded.
    /// </summary>
    /// <param name="loaded">What was loaded from the target.</param>
    /// <param name="targetPath">The solution or project the compilations came from.</param>
    /// <param name="options">The options shaping the generated document.</param>
    /// <returns>The <see cref="GeneratedScreenplay"/>.</returns>
    /// <remarks>
    /// Kept apart from loading so that generating can be exercised against a compilation built from source. Loading
    /// one from disk needs an MSBuild workspace, and standing that up is neither quick nor reliable enough to put
    /// in front of the only check that the generator and the compiler it is built against still agree.
    /// </remarks>
    internal static GeneratedScreenplay GenerateFrom(LoadedCompilation loaded, string targetPath, ScreenplayGenerationOptions options)
    {
        if (loaded.ProjectSourceAlignmentFailureResultFor(targetPath) is { } alignmentFailure)
        {
            return alignmentFailure;
        }

        if (loaded.Compilations.Count == 0)
        {
            return new GeneratedScreenplay(string.Empty, loaded.Diagnostics);
        }

        var result = new ScreenplayGenerator().Generate(
            loaded.Compilations,
            new ScreenplayOptions
            {
                Domain = options.Domain ?? DomainFrom(targetPath, loaded),
                Module = options.Module,
                SegmentsToSkip = options.SegmentsToSkip,
                ModulesFromNamespaceRoots = options.ModulesFromNamespaceRoots
            });

        return new GeneratedScreenplay(
            result.Source,
            [.. loaded.Diagnostics, .. result.Diagnostics.Select(Map)])
        {
            Projects = loaded.ProjectNames
        };
    }

    /// <summary>
    /// Gets the domain to use when none was given.
    /// </summary>
    /// <param name="targetPath">The solution or project that was read.</param>
    /// <param name="loaded">What was loaded from it.</param>
    /// <returns>The domain name, or <see langword="null"/> to leave the choice to the generator.</returns>
    /// <remarks>
    /// The generator names the domain after the assembly, which it can only do when it read exactly one — several
    /// projects have no single assembly to name, and the fallback name describes nobody's application. The solution
    /// is the name the application already goes by, and <c>--domain</c> still overrides it.
    /// </remarks>
    static string? DomainFrom(string targetPath, LoadedCompilation loaded) =>
        loaded.Compilations.Count > 1 ? Path.GetFileNameWithoutExtension(targetPath) : null;

    static ScreenplayDiagnostic Map(Cratis.Arc.Screenplay.ScreenplayDiagnostic diagnostic) =>
        new(
            (ScreenplayDiagnosticSeverity)(int)diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Location);
}
