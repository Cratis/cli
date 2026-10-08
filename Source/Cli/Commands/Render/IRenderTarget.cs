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

    /// <summary>Plans with the exact compiler requirements, verified bodies and attachment-loader diagnostics.</summary>
    /// <param name="compilation">The semantic compilation, including syntax-only authoring metadata.</param>
    /// <param name="executionPlan">The admitted execution plan.</param>
    /// <param name="projectName">The generated project name.</param>
    /// <param name="rootNamespace">The generated root namespace.</param>
    /// <param name="requirements">Requirements emitted by this compilation.</param>
    /// <param name="contents">Resolved bodies keyed by requirement id.</param>
    /// <param name="typedContextDescriptors">Typed contexts from the same compilation as the requirements.</param>
    /// <param name="attachmentDiagnostics">File attachment warnings.</param>
    /// <returns>The immutable artifact plan.</returns>
    ArtifactRenderPlan Plan(
        SemanticCompilation compilation,
        SemanticExecutionPlan executionPlan,
        string? projectName,
        string? rootNamespace,
        ImmutableArray<SemanticImplementationRequirement> requirements,
        ImmutableDictionary<string, string> contents,
        ImmutableArray<SemanticTypedContextDescriptor> typedContextDescriptors,
        ImmutableArray<Diagnostic> attachmentDiagnostics);
}
