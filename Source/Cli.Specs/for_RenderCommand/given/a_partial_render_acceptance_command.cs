// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_RenderCommand.given;

public class a_partial_render_acceptance_command : a_render_acceptance_command
{
    static ArtifactRenderRequest CreateRequest()
    {
        var identity = ApplicationIdentity.Create("MyApp");
        var application = new SemanticApplication(SemanticId.Create(SemanticAddress.ForApplication(identity)), "MyApp", [], [], []);
        var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
        var execution = SemanticExecutionPlan.Compile(model).Plan!;
        var profile = ArtifactRenderProfile.Create("cratis", "1", "Cratis.Stage.Rendering.Cratis", "1", []);
        return new(model, execution, profile, new(ArtifactRenderScopeKind.Application, application.Id));
    }

    static ArtifactRenderPlan CreatePartialPlan()
    {
        var request = CreateRequest();
        var stub = new ArtifactImplementationStub("gap.cs", "MyApp.Payload", "Handle", "static void Handle()", new(0, 1), new("MyApp.Context", []), []);
        var gap = new ArtifactImplementationGap(
            ArtifactImplementationGapOrigin.Described,
            "mock-requirement",
            request.Model.Application.Id.ToString(),
            SemanticImplementationRole.CommandHandler,
            ["Implement the payload."],
            1,
            1,
            [stub]);
        return ArtifactRenderPlan.Create(request, [PlannedArtifact.CreateText("gap.cs", " ")], [], [gap]);
    }

    void Establish()
    {
        _artifactPlan = CreatePartialPlan();
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ScreenplayRenderPlan(1, [], _artifactPlan));
    }
}
