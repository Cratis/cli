// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.Commands.Render.Axon;

/// <summary>
/// Carries only target-neutral contracts across the Axon package boundary.
/// </summary>
/// <param name="Model">The compiled semantic model.</param>
/// <param name="ExecutionPlan">The compiled execution plan.</param>
/// <param name="Scope">The first selected scope, or the application.</param>
internal sealed record AxonRenderInput(
    ExecutableSemanticModel Model,
    SemanticExecutionPlan ExecutionPlan,
    ArtifactRenderScope Scope)
{
    /// <summary>Gets the optional Java project/artifact name.</summary>
    public string? ProjectName { get; init; }

    /// <summary>Gets the optional Java package/group name.</summary>
    public string? JavaPackage { get; init; }

    /// <summary>Gets additional selected scopes.</summary>
    public ImmutableArray<ArtifactRenderScope> AdditionalScopes { get; init; } = [];

    /// <summary>Gets typed implementation requirements.</summary>
    public ImmutableArray<SemanticImplementationRequirement> ImplementationRequirements { get; init; } = [];

    /// <summary>Gets attached implementation source contents.</summary>
    public ImmutableDictionary<string, string> ImplementationContents { get; init; } = [];

    /// <summary>Gets typed context descriptors.</summary>
    public ImmutableArray<SemanticTypedContextDescriptor> TypedContextDescriptors { get; init; } = [];

    /// <summary>Gets attachment diagnostics without filtering.</summary>
    public ImmutableArray<Diagnostic> AttachmentDiagnostics { get; init; } = [];

    /// <summary>Gets release-baseline assessment.</summary>
    public ReleaseBaselineAssessment ReleaseBaseline { get; init; } = ReleaseBaselineAssessment.NotSupplied;

    /// <summary>Gets plain-text file-header lines.</summary>
    public ImmutableArray<string> FileHeader { get; init; } = [];

    /// <summary>Gets whether unmeasured intent is refused instead of warned about.</summary>
    public bool Strict { get; init; }
}
