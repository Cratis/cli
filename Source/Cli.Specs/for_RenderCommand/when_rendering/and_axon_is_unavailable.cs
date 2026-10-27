// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Cli.Commands.Screenplay;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_axon_is_unavailable : given.a_check_command
{
    (int ExitCode, string Stdout, string Stderr) _output;

    void Establish()
    {
        _command = new(_planning, _publication);
        _settings.Target = "axon";
        _settings.Check = false;
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
            new ScreenplayPlanning().Plan(call.Arg<ScreenplayRenderRequest>(), call.Arg<CancellationToken>()));
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_return_validation_error() => _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_one_experimental_warning() => _output.Stderr.Split("CLI-RENDER-029", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_report_the_unavailable_renderer() => _output.Stderr.ShouldContain("CLI-RENDER-028");
    [Fact] void should_not_publish() => _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_create_the_destination() => Directory.Exists(_destination).ShouldBeFalse();
}
