// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Defines a system that generates a Screenplay document from application source code.
/// </summary>
/// <remarks>
/// This is the seam between the CLI and source framework generator packages. Everything the CLI does around
/// generation — resolving the target, writing the document, reporting diagnostics — is expressed against this
/// interface so that it stays independent of how framework semantics are recovered.
/// </remarks>
public interface IScreenplayGeneration
{
    /// <summary>
    /// Generates the Screenplay document describing the application in the given solution or project.
    /// </summary>
    /// <param name="targetPath">The full path of the solution or project file to read.</param>
    /// <param name="options">The options that shape the generated document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="GeneratedScreenplay"/> holding the source and any diagnostics.</returns>
    Task<GeneratedScreenplay> Generate(string targetPath, ScreenplayGenerationOptions options, CancellationToken cancellationToken);
}
