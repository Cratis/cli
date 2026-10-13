// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Reads authored sources and classifies Screenplay-owned structural comparison results.
/// </summary>
public interface IScreenplayConformance
{
    /// <summary>
    /// Compiles a root file and its imports, or a model folder, without writing files.
    /// </summary>
    /// <param name="modelRoot">The resolved model root.</param>
    /// <returns>The authored source snapshot and source diagnostics.</returns>
    AuthoredScreenplay Read(string modelRoot);

    /// <summary>
    /// Compares the authored baseline with generated code source by exact addresses.
    /// </summary>
    /// <param name="model">The authored baseline.</param>
    /// <param name="code">The source recovered from code.</param>
    /// <returns>The public Screenplay structural difference.</returns>
    ModelDifference Compare(AuthoredScreenplay model, string code);
}
