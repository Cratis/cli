// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Axon;
using Cratis.Cli.Commands.Render.Publication;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Axon;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_axon_reports_degradation : given.a_check_command
{
    void Establish()
    {
        File.WriteAllText(_document, "module Library\n");
        _settings.Target = "axon";
        _settings.Check = false;
        var rendering = Substitute.For<IAxonRendering>();
        rendering.Target.Returns("axon");
        rendering.NewestAdmittedSemanticVersion.Returns(AxonRendering.NewestAdmittedSemanticVersion);
        rendering.Plan(Arg.Any<AxonRenderInput>()).Returns(call =>
        {
            var input = call.Arg<AxonRenderInput>();
            var request = new ArtifactRenderRequest(input.Model, input.ExecutionPlan, AxonRendering.CreateProfile(), input.Scope);
            var warning = new ArtifactRenderDiagnostic("STAGE-AXON-0308", ArtifactRenderDiagnosticSeverity.Warning, "Persistent profile degrades to in-memory storage.", input.Model.Application.Id);
            var degradation = warning with { Code = ArtifactRenderDegradationCodes.UnsupportedConstruct };
            return ArtifactRenderPlan.Create(request, [PlannedArtifact.CreateText("Main.java", "// in-memory fallback\n")], [warning, degradation], [], [], [])
                .WithAcceptance([new(degradation, "Configure persistent storage.")]);
        });
        _planning = new ScreenplayPlanning(new SemanticModelCompiler(), new RenderTargetRoster([new AxonRenderTarget(rendering)]));
        _command = new RenderCommand(_planning, new ArtifactPublisher());
    }

    [Theory]
    [InlineData(false, ExitCodes.Success)]
    [InlineData(true, ExitCodes.StrictUnaccepted)]
    public async Task should_publish_safe_output_and_only_reject_acceptance_in_strict_mode(bool strict, int expectedExit)
    {
        _settings.Strict = strict;

        var output = await Capture();

        Assert.True(output.ExitCode == expectedExit, output.Stderr);
        output.Stderr.ShouldContain("STAGE-AXON-0308");
        (await File.ReadAllTextAsync(Path.Combine(_destination, "Main.java"), TestContext.Current.CancellationToken)).ShouldEqual("// in-memory fallback\n");
        File.Exists(Path.Combine(_destination, ".cratis-render.json")).ShouldBeTrue();
    }
}
