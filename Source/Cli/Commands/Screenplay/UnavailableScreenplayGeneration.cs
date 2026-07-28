// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Reports that the <c>Cratis.Arc.Screenplay</c> generator is not part of this build of the CLI.
/// </summary>
/// <remarks>
/// The generator package is not published yet. Until it is, the CLI is built against it only when
/// <c>CratisArcScreenplayProject</c> points at a local checkout of it — see <c>Cli.csproj</c>. Reporting the gap as
/// an error diagnostic keeps the command's contract intact rather than failing in some other, less explicable way.
/// </remarks>
public sealed class UnavailableScreenplayGeneration : IScreenplayGeneration
{
    /// <inheritdoc/>
    public Task<GeneratedScreenplay> Generate(string targetPath, ScreenplayGenerationOptions options, CancellationToken cancellationToken) =>
        Task.FromResult(GeneratedScreenplay.Failed(
            ScreenplayDiagnosticCodes.GeneratorUnavailable,
            "This build of the CLI does not include the Cratis.Arc.Screenplay generator"));
}
