// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Defines a system that renders Screenplay documents into a working application.
/// </summary>
/// <remarks>
/// The seam between the CLI and the Stage renderer, mirroring <see cref="IScreenplayValidation"/> on the other
/// arrow. Everything the CLI does around rendering — resolving the path, reporting, deciding the exit code — is
/// expressed against this interface so it stays independent of which target is rendered into.
/// </remarks>
public interface IScreenplayRendering
{
    /// <summary>
    /// Renders the Screenplay document, or every document beneath the folder, into the target directory.
    /// </summary>
    /// <param name="targetPath">The full path of a <c>.play</c> file, or of a folder to search.</param>
    /// <param name="outputDirectory">The full path of the directory to render into.</param>
    /// <returns>The <see cref="RenderedScreenplay"/> holding what was rendered and what could not be.</returns>
    Task<RenderedScreenplay> Render(string targetPath, string outputDirectory);
}
