// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_refused_strict_acceptance : given.a_render_acceptance_command
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_report_all_conditions_but_prioritize_integrity(bool recoveryPending)
    {
        _settings.Check = true;
        _settings.Strict = true;
        ArtifactPublicationVerdict[] changes =
        [
            new("pending.cs", "write", null, "new-hash"),
            new(null, "refused", null, null, "Unsafe destination") { Code = "MOCK-INTEGRITY" }
        ];
        var receipt = new ArtifactPublicationCheckReceipt(changes, new("manifest-hash", "new-manifest-hash"));
        _publication.Check(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ArtifactPublicationCheckResult(1, 0, 0, 1, recoveryPending, receipt));
        var before = Snapshot(_destination);

        var output = await Capture();

        output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
        output.Stdout.ShouldContain("pending.cs");
        output.Stdout.ShouldContain("refused");
        output.Stderr.ShouldContain("MOCK-INTEGRITY");
        output.Stderr.ShouldContain(ExitCodes.StrictUnacceptedCode);
        Snapshot(_destination).ShouldEqual(before);
        await _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
        await _publication.DidNotReceive().Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _acceptance.Received(1)(Arg.Any<ScreenplayRenderPlan>(), true);
    }
}
