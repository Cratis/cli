// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_CratisRenderTarget.given;

/// <summary>
/// Base context that compiles a Screenplay document straight to an admitted execution plan, without going
/// through <see cref="ScreenplayPlanning"/>, so the bundled <see cref="CratisRenderTarget"/> can be exercised
/// in isolation against the published <see cref="CratisRendering"/> facade.
/// </summary>
public class a_cratis_render_target : Specification
{
    /// <summary>
    /// The published canonical source, including validation and success/rejection specifications.
    /// </summary>
    protected static readonly string SupportedSource = RegisterProjectCorpus.LegacyV1.SourceForms
        .Single(_ => _.Name == "single").Documents.Single().Text;

    /// <summary>
    /// A slice the model compiler and execution planner admit, but the published Cratis facade cannot render:
    /// a produced event with no affected-instance mapping (missing "for projectId").
    /// </summary>
    protected static readonly string UnsupportedSource = Lines(
        "concept ProjectId : Uuid",
        "module Projects",
        "  feature Registration",
        "    slice StateChange RegisterProject",
        "      command RegisterProject",
        "        projectId ProjectId identifier",
        "        produces ProjectRegistered",
        "          projectId = projectId",
        "      event ProjectRegistered",
        "        projectId ProjectId");

    private protected CratisRenderTarget _target = null!;

    void Establish() => _target = new();

    static string Lines(params string[] lines) => string.Join('\n', lines);

    /// <summary>
    /// Compiles a document straight to an admitted execution plan.
    /// </summary>
    /// <param name="source">The Screenplay document source.</param>
    /// <param name="applicationName">The application name.</param>
    /// <returns>The compiled model and its admitted execution plan.</returns>
    private protected static (ExecutableSemanticModel Model, SemanticExecutionPlan ExecutionPlan) Compile(string source, string applicationName = "Projects")
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(applicationName));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("Projects.play"), "Projects.play", "Projects.play", source);
        var compilation = new SemanticModelCompiler().Compile(applicationName, SemanticDocumentSet.Create([document], catalog));
        var model = compilation.Value!.Model;
        var executionPlan = SemanticExecutionPlan.Compile(model).Plan!;
        return (model, executionPlan);
    }

    /// <summary>
    /// Plans the exact same request directly through the published facade, for comparison against the CLI target.
    /// </summary>
    /// <param name="model">The compiled model.</param>
    /// <param name="executionPlan">The admitted execution plan.</param>
    /// <returns>The facade's own plan.</returns>
    private protected static ArtifactRenderPlan PlanWithFacade(ExecutableSemanticModel model, SemanticExecutionPlan executionPlan) =>
        CratisRendering.Plan(
            model,
            executionPlan,
            new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id),
            new CratisRenderingOptions(model.Application.Name, model.Application.Name));
}
