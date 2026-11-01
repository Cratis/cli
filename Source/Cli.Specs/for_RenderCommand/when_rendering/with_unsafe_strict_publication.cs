// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_unsafe_strict_publication : given.a_render_acceptance_command
{
    (int ExitCode, string Stdout, string Stderr) _output;

    void Establish()
    {
        _settings.Strict = true;
        _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ArtifactPublicationResult>(new UnsafeArtifactPublication("Unmanaged collision")));
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_prioritize_integrity_failure() => _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_claim_publication() => _output.Stdout.ShouldEqual(string.Empty);
    [Fact] void should_not_create_the_destination() => Directory.Exists(_destination).ShouldBeFalse();
    [Fact] void should_not_run_acceptance_after_failed_publication() => _acceptance.DidNotReceive()(Arg.Any<ScreenplayRenderPlan>(), Arg.Any<bool>());
}
