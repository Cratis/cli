// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Render.Publication;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using NSubstitute.Extensions;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_safe_empty_artifacts_and_rejected_acceptance : given.a_render_acceptance_command
{
    (int ExitCode, string Stdout, string Stderr) _output;

    void Establish()
    {
        _settings.Strict = true;
        var identity = ApplicationIdentity.Create("MyApp");
        var application = new SemanticApplication(SemanticId.Create(SemanticAddress.ForApplication(identity)), "MyApp", [], [], []);
        var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
        var execution = SemanticExecutionPlan.Compile(model).Plan!;
        var profile = ArtifactRenderProfile.Create("cratis", "1", "Cratis.Stage.Rendering.Cratis", "1", []);
        _artifactPlan = ArtifactRenderPlan.Create(new(model, execution, profile, new(ArtifactRenderScopeKind.Application, application.Id)), [], []);
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ScreenplayRenderPlan(1, [], _artifactPlan));
        _publication.Configure().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ArtifactPublicationResult(0, 0, 0, new([], new(null, "manifest-hash")), false));
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_have_a_safe_plan() => _artifactPlan.Success.ShouldBeTrue();
    [Fact] void should_have_no_planned_artifacts() => _artifactPlan.Artifacts.ShouldBeEmpty();
    [Fact] void should_fail_acceptance() => _output.ExitCode.ShouldEqual(ExitCodes.StrictUnaccepted);
    [Fact] void should_report_acceptance_failure() => _output.Stderr.ShouldContain(AcceptanceFailure);
    [Fact] void should_emit_the_strict_unaccepted_machine_code() => _output.Stderr.ShouldContain(ExitCodes.StrictUnacceptedCode);
    [Fact] void should_not_create_artifact_or_report_files() => Directory.Exists(_destination).ShouldBeFalse();
    [Fact] void should_publish_the_empty_plan_before_acceptance() => Received.InOrder(() =>
    {
        _publication.Publish(Arg.Is<ArtifactPublicationRequest>(request => request.Plan == _artifactPlan), Arg.Any<CancellationToken>());
        _acceptance(Arg.Any<ScreenplayRenderPlan>(), true);
    });

    [Fact]
    void should_emit_a_zero_write_publication_receipt()
    {
        using var json = JsonDocument.Parse(_output.Stdout);
        json.RootElement.GetProperty("publication").GetProperty("status").GetString().ShouldEqual("published");
        json.RootElement.GetProperty("written").GetInt32().ShouldEqual(0);
        json.RootElement.GetProperty("artifacts").GetInt32().ShouldEqual(0);
    }
}
