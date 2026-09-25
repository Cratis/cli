// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
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
    /// <param name="projectName">The generated project name, defaulting to the application name.</param>
    /// <param name="rootNamespace">The requested root namespace, defaulting to the application name.</param>
    /// <returns>The immutable artifact plan.</returns>
    ArtifactRenderPlan Plan(ExecutableSemanticModel model, SemanticExecutionPlan executionPlan, string? projectName = null, string? rootNamespace = null);

    /// <summary>Plans with the exact compiler requirements, verified bodies and attachment-loader diagnostics.</summary>
    /// <param name="model">The executable model.</param>
    /// <param name="executionPlan">The admitted execution plan.</param>
    /// <param name="projectName">The generated project name.</param>
    /// <param name="rootNamespace">The generated root namespace.</param>
    /// <param name="requirements">Requirements emitted by this compilation.</param>
    /// <param name="contents">Resolved bodies keyed by requirement id.</param>
    /// <param name="attachmentDiagnostics">File attachment warnings.</param>
    /// <returns>The immutable artifact plan.</returns>
    ArtifactRenderPlan Plan(
        ExecutableSemanticModel model,
        SemanticExecutionPlan executionPlan,
        string? projectName,
        string? rootNamespace,
        ImmutableArray<SemanticImplementationRequirement> requirements,
        ImmutableDictionary<string, string> contents,
        ImmutableArray<Diagnostic> attachmentDiagnostics);
}
