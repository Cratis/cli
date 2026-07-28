// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Defines a system that compiles Screenplay documents and reports what the compiler found.
/// </summary>
/// <remarks>
/// This is the seam between the CLI and the <c>Cratis.Screenplay</c> compiler, mirroring
/// <see cref="IScreenplayGeneration"/>. Everything the CLI does around validation — resolving the path, reporting
/// diagnostics, deciding the exit code — is expressed against this interface so that it stays independent of how a
/// document is compiled.
/// </remarks>
public interface IScreenplayValidation
{
    /// <summary>
    /// Compiles the Screenplay document, or every document beneath the folder, at the given path.
    /// </summary>
    /// <param name="targetPath">The full path of a <c>.play</c> file, or of a folder to search.</param>
    /// <returns>The <see cref="ValidatedScreenplay"/> holding what was compiled and any diagnostics.</returns>
    ValidatedScreenplay Validate(string targetPath);
}
