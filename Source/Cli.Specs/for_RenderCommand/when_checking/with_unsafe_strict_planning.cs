// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_unsafe_strict_planning : given.a_render_acceptance_command
{
    [Theory]
    [InlineData(0, ExitCodes.NotFound)]
    [InlineData(1, ExitCodes.ValidationError)]
    public async Task should_prioritize_planning_errors_without_writes(int documents, int expectedExit)
    {
        _settings.Check = true;
        _settings.Strict = true;
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ScreenplayRenderPlan(documents, [new(ScreenplayDiagnosticSeverity.Error, "MOCK-UNSAFE", "Unsafe generation", null)], null));
        var before = Snapshot(_destination);

        var output = await Capture();

        output.ExitCode.ShouldEqual(expectedExit);
        output.Stdout.ShouldEqual(string.Empty);
        Snapshot(_destination).ShouldEqual(before);
        await _publication.DidNotReceive().Check(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
        await _publication.DidNotReceive().Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
        _acceptance.DidNotReceive()(Arg.Any<ScreenplayRenderPlan>(), Arg.Any<bool>());
    }
}
