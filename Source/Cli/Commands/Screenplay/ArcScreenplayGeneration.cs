// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Generates a Screenplay document by loading the target into a Roslyn compilation and handing it to the
/// <c>Cratis.Arc.Screenplay</c> generator.
/// </summary>
/// <remarks>
/// This is the only place in the CLI that knows the generator exists. Everything else is expressed against
/// <see cref="IScreenplayGeneration"/>.
/// </remarks>
public sealed class ArcScreenplayGeneration : IScreenplayGeneration
{
    /// <inheritdoc/>
    public async Task<GeneratedScreenplay> Generate(string targetPath, ScreenplayGenerationOptions options, CancellationToken cancellationToken)
    {
        var loaded = await ScreenplayCompilationLoader.Load(targetPath, cancellationToken);
        if (loaded.Compilation is null)
        {
            return new GeneratedScreenplay(string.Empty, loaded.Diagnostics);
        }

        var result = new ScreenplayGenerator().Generate(
            loaded.Compilation,
            new ScreenplayOptions
            {
                Domain = options.Domain,
                Module = options.Module,
                SegmentsToSkip = options.SegmentsToSkip
            });

        return new GeneratedScreenplay(
            result.Source,
            [.. loaded.Diagnostics, .. result.Diagnostics.Select(Map)]);
    }

    static ScreenplayDiagnostic Map(Cratis.Arc.Screenplay.ScreenplayDiagnostic diagnostic) =>
        new(
            (ScreenplayDiagnosticSeverity)(int)diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Location);
}
