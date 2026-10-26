// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Engine.Packages;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.Scene;

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
        ICollection<ScreenplayDiagnostic> warnings,
        SceneApplication? scene = null,
        RenderSelection? selection = null,
        ApprovedPackageSet? approvedPackages = null,
        ReleaseBaselineAssessment? releaseBaseline = null,
        ArtifactRenderInput? renderingInput = null,
        IReadOnlyList<ReducerMigrationEvidence>? migrations = null,
        ImmutableArray<string> fileHeader = default)
    {
        var model = compilation.Model;
        var options = CratisRenderAdmission.RenderingOptions(model.Application.Name, projectName, rootNamespace, fileHeader);
        var profile = WithAuthoringMetadata(CreateProfile(model.Application.Name, options, scene, warnings, approvedPackages, renderingInput, migrations), compilation, warnings);
        var scope = selection?.Scopes[0] ?? new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id);
        return planner.Plan(new ArtifactRenderRequest(model, executionPlan, profile, scope)
        {
            AdditionalScopes = selection?.Scopes.RemoveAt(0) ?? [],
            ImplementationRequirements = requirements,
            ImplementationContents = contents,
            TypedContextDescriptors = typedContextDescriptors,
            AttachmentDiagnostics = attachmentDiagnostics,
            ReleaseBaseline = releaseBaseline ?? ReleaseBaselineAssessment.NotSupplied
        });
    }

    /// <summary>
    /// Gets whether the authored Scene lists PrimeReact as a package of any of its ui profiles.
    /// </summary>
    /// <param name="scene">The authored Scene, if any.</param>
    /// <returns>True when PrimeReact is listed; otherwise false, meaning the React Aria scaffold.</returns>
    internal static bool UsesPrimeReact(SceneApplication? scene) => scene is not null && UsesPrimeReact(SafeSceneJson(scene));

    /// <summary>
    /// Gets whether the Scene document lists PrimeReact, reading the very scene.json appended to the profile so the scaffold never disagrees with it.
    /// </summary>
    /// <param name="sceneJson">The Scene document, if any.</param>
    /// <returns>True when PrimeReact is listed; otherwise false.</returns>
    internal static bool UsesPrimeReact(string? sceneJson)
    {
        var profiles = sceneJson is null ? null : (JsonNode.Parse(sceneJson) as JsonObject)?["uiProfiles"] as JsonArray;
        var packages = (profiles ?? [])
            .OfType<JsonObject>()
            .Select(profile => profile["packages"] as JsonArray)
            .Where(array => array is not null)
            .SelectMany(array => array!)
            .Select(package => package?.GetValue<string>())
            .OfType<string>();
        return CratisRendering.ListsPrimeReact(packages);
    }

    static ArtifactRenderProfile CreateProfile(
        string applicationName,
        CratisRenderingOptions options,
        SceneApplication? scene,
        ICollection<ScreenplayDiagnostic> warnings,
        ApprovedPackageSet? approvedPackages,
        ArtifactRenderInput? renderingInput,
        IReadOnlyList<ReducerMigrationEvidence>? migrations)
    {
        var sceneJson = scene is null ? null : SafeSceneJson(scene);
        var profile = CratisRendering.CreateProfile(applicationName, options, null, null, null, approvedPackages, UsesPrimeReact(sceneJson));
        if (renderingInput is not null)
        {
            profile = CratisRendering.WithReducerConvention(profile, renderingInput);
        }
        if (migrations is { Count: > 0 })
        {
            // The CLI publisher deletes every required removal atomically with the plan's writes.
            profile = CratisRendering.WithReducerMigration(profile, applicationName, migrations, canRemoveFiles: true);
        }
        if (scene is null)
        {
            return profile;
        }

        foreach (var issue in scene.RuntimeIssues)
        {
            warnings.Add(new(ScreenplayDiagnosticSeverity.Warning, issue.Code, issue.Details, issue.Location.ToString()));
        }

        var sceneInput = CratisArtifactRenderInput.CreateText("scene.json", CratisRendering.TargetVersion, sceneJson!);
        return ArtifactRenderProfile.Create(
            profile.Target,
            profile.TargetVersion,
            profile.Renderer,
            profile.RendererVersion,
            [.. profile.Inputs, sceneInput]);
    }

    static string SafeSceneJson(SceneApplication scene)
    {
        // Sample data exists to preview with at design time (spec SD6). The Scene document is a content file of the rendered
        // application, so a production render keeps no pack, no blueprint pack and no profile selection in it.
        var node = JsonNode.Parse(CanonicalSceneJson.Serialize(scene.ForRuntime())) ?? throw new JsonException("The authored Scene did not serialize to JSON.");
        RemoveUnevaluableGuardedActions(node);
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>
    /// Leaves out the guarded actions the Stage runtime cannot evaluate.
    /// </summary>
    /// <param name="node">The Scene document, or a part of it.</param>
    /// <remarks>
    /// A guarded action the runtime can evaluate is published as authored, with its guard and fallback. Only one it
    /// cannot evaluate is removed, exactly as the Stage runtime's own safe subset does, and it is reported as a warning.
    /// </remarks>
    static void RemoveUnevaluableGuardedActions(JsonNode? node)
    {
        if (node is JsonArray array)
        {
            for (var index = array.Count - 1; index >= 0; index--)
            {
                if (array[index] is JsonObject element && SceneGuards.IsGuardedAction(element) && !SceneGuards.CanRunAction(element["properties"] as JsonObject))
                {
                    array.RemoveAt(index);
                    continue;
                }

                RemoveUnevaluableGuardedActions(array[index]);
            }
        }
        else if (node is JsonObject @object)
        {
            foreach (var property in @object.ToArray())
            {
                RemoveUnevaluableGuardedActions(property.Value);
            }
        }
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
