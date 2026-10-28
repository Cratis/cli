// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_axon_uses_strict_acceptance : given.a_render_acceptance_command
{
    void Establish()
    {
        _settings.Target = "axon";
        _settings.Strict = true;
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ScreenplayRenderPlan(1, [], _artifactPlan) { Target = new AxonRenderTarget() });
    }

    [Theory]
    [InlineData(false, false, false, ExitCodes.StrictUnaccepted)]
    [InlineData(true, true, false, ExitCodes.StrictUnaccepted)]
    [InlineData(true, false, false, ExitCodes.StrictUnaccepted)]
    [InlineData(true, false, true, ExitCodes.ValidationError)]
    public async Task should_apply_strict_acceptance_without_overriding_publication_errors(bool check, bool drift, bool refused, int expectedExit)
    {
        _settings.Check = check;
        _publication.Check(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ArtifactPublicationCheckResult(drift ? 2 : 0, 0, 0, refused ? 1 : 0, false, new([], new("manifest-hash", "manifest-hash"))));

        var output = await Capture();

        output.ExitCode.ShouldEqual(expectedExit);
        output.Stderr.ShouldContain(ExitCodes.StrictUnacceptedCode);
        _acceptance.Received(1)(Arg.Any<ScreenplayRenderPlan>(), true);
        if (check)
        {
            Directory.Exists(_destination).ShouldBeFalse();
            await _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
        }
        else
        {
            (await File.ReadAllTextAsync(_artifactPath, TestContext.Current.CancellationToken)).ShouldEqual("allowed artifact");
            await _publication.Received(1).Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
        }
    }
}
