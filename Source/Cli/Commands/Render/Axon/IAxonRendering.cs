// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.Commands.Render.Axon;

/// <summary>
/// Isolates the CLI from the experimental renderer's package-owned facade.
/// </summary>
internal interface IAxonRendering
{
    /// <summary>Gets the target identity.</summary>
    string Target { get; }

    /// <summary>Gets the renderer's explicit semantic version ceiling.</summary>
    SemanticVersion NewestAdmittedSemanticVersion { get; }

    /// <summary>Plans without writing files.</summary>
    /// <param name="input">The compiler and target inputs.</param>
    /// <returns>The immutable artifact plan, including renderer diagnostics.</returns>
    ArtifactRenderPlan Plan(AxonRenderInput input);
}
