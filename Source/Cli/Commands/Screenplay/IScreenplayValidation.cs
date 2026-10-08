// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Defines a system that compiles Screenplay documents and reports what the compiler found.
/// </summary>
/// <remarks>
/// This is the seam between the CLI and the <c language="csharp">Cratis.Screenplay</c> compiler, mirroring
/// <see cref="IScreenplayGeneration"/>. Everything the CLI does around validation — resolving the path, reporting
/// diagnostics, deciding the exit code — is expressed against this interface so that it stays independent of how a
/// document is compiled.
/// </remarks>
public interface IScreenplayValidation
{
    /// <summary>
    /// Compiles the Screenplay document, or every document beneath the folder, at the given path.
    /// </summary>
    /// <param name="targetPath">The full path of a <c language="csharp">.play</c> file, or of a folder to search.</param>
    /// <returns>The <see cref="ValidatedScreenplay"/> holding what was compiled and any diagnostics.</returns>
    ValidatedScreenplay Validate(string targetPath);

    /// <summary>
    /// Compiles the Screenplay document, or every document beneath the folder, and binds the result into an executable semantic model.
    /// </summary>
    /// <param name="targetPath">The full path of a <c language="csharp">.play</c> file, or of a folder to search.</param>
    /// <returns>The <see cref="ValidatedScreenplay"/> holding the compilation and binding diagnostics and whether the model binds.</returns>
    ValidatedScreenplay ValidateExecutable(string targetPath);

    /// <summary>
    /// Compiles the documents and reports selected structural completeness warnings when source compilation succeeds.
    /// </summary>
    /// <param name="targetPath">The full path of a document or folder.</param>
    /// <param name="checks">The completeness checks to run.</param>
    /// <returns>The compilation diagnostics, completeness warnings and check status.</returns>
    ValidatedScreenplay Validate(string targetPath, CompletenessChecks checks);

    /// <summary>
    /// Compiles the documents, checks selected structural completeness and binds into an executable semantic model.
    /// </summary>
    /// <param name="targetPath">The full path of a document or folder.</param>
    /// <param name="checks">The completeness checks to run when source compilation succeeds.</param>
    /// <returns>The compilation, completeness and binding diagnostics and their verdicts.</returns>
    ValidatedScreenplay ValidateExecutable(string targetPath, CompletenessChecks checks);
}
