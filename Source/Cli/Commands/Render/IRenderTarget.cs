// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Defines one statically reviewed renderer target bundled with the CLI.
/// </summary>
internal interface IRenderTarget
{
    /// <summary>
    /// Gets the stable command-line target name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plans target artifacts from an admitted ESM execution plan.
    /// </summary>
    /// <param name="model">The executable semantic model.</param>
    /// <param name="executionPlan">The admitted execution plan.</param>
    /// <returns>The immutable artifact plan.</returns>
    ArtifactRenderPlan Plan(ExecutableSemanticModel model, SemanticExecutionPlan executionPlan);
}
