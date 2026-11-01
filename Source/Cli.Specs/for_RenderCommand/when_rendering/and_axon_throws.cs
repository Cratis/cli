// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Axon;
using Cratis.Cli.Commands.Render.Publication;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Axon;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_axon_throws : given.a_check_command
{
    (int ExitCode, string Stdout, string Stderr) _output;

    void Establish()
    {
        File.WriteAllText(_document, "module Library\n");
        _settings.Target = "axon";
        _settings.Check = false;
        var rendering = Substitute.For<IAxonRendering>();
        rendering.Target.Returns("axon");
        rendering.NewestAdmittedSemanticVersion.Returns(AxonRendering.NewestAdmittedSemanticVersion);
        rendering.Plan(Arg.Any<AxonRenderInput>()).Returns(_ => throw new InvalidOperationException("Unexpected renderer failure"));
        _planning = new ScreenplayPlanning(new SemanticModelCompiler(), new RenderTargetRoster([new AxonRenderTarget(rendering)]));
        _command = new RenderCommand(_planning, _publication);
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_return_a_validation_error() => _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_clear_diagnostic() => _output.Stderr.ShouldContain(AxonRenderingGuard.RendererFailureCode);
    [Fact] void should_name_the_target_and_renderer_version() => _output.Stderr.ShouldContain($"axon renderer Cratis.Stage.Rendering.Axon {AxonRendering.Version}");
    [Fact] void should_report_the_exception_type_and_message() => _output.Stderr.ShouldContain("InvalidOperationException: Unexpected renderer failure");
    [Fact] void should_explain_that_this_is_a_renderer_bug() => _output.Stderr.ShouldContain("this is a renderer bug; please report it at https://github.com/Cratis/Stage.Axon/issues");
    [Fact] void should_not_include_a_stack_trace() => _output.Stderr.ShouldNotContain("   at ");
    [Fact] void should_not_create_the_destination() => Directory.Exists(_destination).ShouldBeFalse();
    [Fact] Task should_not_publish() => _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
}
