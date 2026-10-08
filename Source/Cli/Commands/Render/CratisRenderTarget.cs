// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Represents the statically bundled Cratis ESM renderer target, using the published
/// <see cref="CratisRendering"/> profile policy and package-owned artifact planner.
/// </summary>
/// <param name="planner">The artifact planner consuming the complete package-owned profile.</param>
internal sealed class CratisRenderTarget(IArtifactRenderPlanner planner) : IRenderTarget
{
    /// <summary>
    /// The diagnostic code reported when authored documentation cannot be read and is left out of the rendered code.
    /// </summary>
    public const string AuthoringMetadataSkippedCode = "CLI-RENDER-006";

    /// <summary>
    /// Initializes a new instance of the <see cref="CratisRenderTarget"/> class with the shipped planner.
    /// </summary>
    public CratisRenderTarget()
        : this(new CratisArtifactRenderPlanner())
    {
    }

    /// <inheritdoc/>
    public string Name => CratisRendering.TargetId;

    /// <inheritdoc/>
    public ArtifactRenderPlan Plan(
        SemanticCompilation compilation,
        SemanticExecutionPlan executionPlan,
        string? projectName,
        string? rootNamespace,
        ImmutableArray<SemanticImplementationRequirement> requirements,
        ImmutableDictionary<string, string> contents,
        ImmutableArray<SemanticTypedContextDescriptor> typedContextDescriptors,
        ImmutableArray<Diagnostic> attachmentDiagnostics,
        ICollection<ScreenplayDiagnostic> warnings)
    {
        var model = compilation.Model;
        var options = new CratisRenderingOptions(projectName ?? model.Application.Name, rootNamespace ?? model.Application.Name);
        var profile = WithAuthoringMetadata(CratisRendering.CreateProfile(model.Application.Name, options), compilation, warnings);
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id);
        return planner.Plan(new ArtifactRenderRequest(model, executionPlan, profile, scope)
        {
            ImplementationRequirements = requirements,
            ImplementationContents = contents,
            TypedContextDescriptors = typedContextDescriptors,
            AttachmentDiagnostics = attachmentDiagnostics
        });
    }

    /// <summary>
    /// Adds the authored documentation, unless the documents cannot form the workspace Stage reads it from.
    /// </summary>
    /// <param name="profile">The profile without documentation.</param>
    /// <param name="compilation">The compilation holding the authored metadata.</param>
    /// <param name="warnings">Receives the reason when the documentation is left out.</param>
    /// <returns>The profile, with documentation when it could be read.</returns>
    /// <remarks>
    /// Stage reads the metadata from a Screenplay workspace, whose path rules are stricter than the ones
    /// <c language="shell">cratis render</c> accepts (for example an upper-case <c language="shell">.PLAY</c> extension).
    /// Documentation is optional output, so such a model still renders as before, without comments, and says why.
    /// </remarks>
    static ArtifactRenderProfile WithAuthoringMetadata(ArtifactRenderProfile profile, SemanticCompilation compilation, ICollection<ScreenplayDiagnostic> warnings)
    {
        try
        {
            return CratisRendering.WithAuthoringMetadata(profile, compilation);
        }
        catch (Exception exception) when (exception is InvalidPortablePlayPath or InvalidScreenplayWorkspace or InvalidWorkspaceDocument)
        {
            warnings.Add(new(
                ScreenplayDiagnosticSeverity.Warning,
                AuthoringMetadataSkippedCode,
                $"Authored descriptions and documentation were not rendered as code comments: {exception.Message}",
                null));
            return profile;
        }
    }
}
