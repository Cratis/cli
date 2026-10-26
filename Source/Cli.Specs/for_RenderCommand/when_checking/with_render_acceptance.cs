// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_render_acceptance : given.a_render_acceptance_command
{
    [Theory]
    [InlineData(false, false, true, ExitCodes.ChangesPending)]
    [InlineData(true, false, true, ExitCodes.ValidationError)]
    [InlineData(true, false, false, ExitCodes.ValidationError)]
    [InlineData(true, true, true, ExitCodes.ChangesPending)]
    [InlineData(true, true, false, ExitCodes.Success)]
    public async Task should_check_without_writes_and_prioritize_acceptance(bool strict, bool accepted, bool drift, int expectedExit)
    {
        _settings.Strict = strict;
        _settings.Check = true;
        _acceptance(Arg.Any<ScreenplayRenderPlan>(), Arg.Any<bool>())
            .Returns(call => call.ArgAt<bool>(1) && !accepted ? AcceptanceFailure : null);
        _publication.Check(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ArtifactPublicationCheckResult(drift ? 2 : 0, 0, drift ? 0 : 2, 0, false, new([], new("manifest-hash", drift ? "new-manifest-hash" : "manifest-hash"))));
        var before = Snapshot(_destination);

        var output = await Capture();

        output.ExitCode.ShouldEqual(expectedExit);
        Snapshot(_destination).ShouldEqual(before);
        output.Stdout.ShouldContain("check");
        await _publication.DidNotReceive().Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
        _acceptance.Received(1)(Arg.Any<ScreenplayRenderPlan>(), strict);
    }
}
