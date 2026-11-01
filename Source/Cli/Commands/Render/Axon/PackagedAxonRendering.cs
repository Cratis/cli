// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Axon;

namespace Cratis.Cli.Commands.Render.Axon;

/// <summary>
/// Maps CLI compiler inputs to the published Axon rendering facade.
/// </summary>
internal sealed class PackagedAxonRendering : IAxonRendering
{
    /// <inheritdoc/>
    public string Target => AxonRendering.Target;

    /// <inheritdoc/>
    public SemanticVersion NewestAdmittedSemanticVersion => AxonRendering.NewestAdmittedSemanticVersion;

    /// <inheritdoc/>
    public ArtifactRenderPlan Plan(AxonRenderInput input) => AxonRendering.Plan(new(input.Model, input.ExecutionPlan, input.Scope)
    {
        AdditionalScopes = input.AdditionalScopes,
        ImplementationRequirements = input.ImplementationRequirements,
        ImplementationContents = input.ImplementationContents,
        TypedContextDescriptors = input.TypedContextDescriptors,
        AttachmentDiagnostics = input.AttachmentDiagnostics,
        ReleaseBaseline = input.ReleaseBaseline,
        Options = new()
        {
            ProjectName = input.ProjectName,
            JavaPackage = input.JavaPackage,
            FileHeader = input.FileHeader,
            Strict = input.Strict
        }
    });
}
