// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_a_planning_failure : given.a_check_command
{
    (int ExitCode, string Stdout, string Stderr) _output;
    string[] _before = [];

    void Establish()
    {
        _command = new RenderCommand(_planning, _publication);
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>()).Returns(new ScreenplayRenderPlan(
            1, [new(ScreenplayDiagnosticSeverity.Error, "PLAY0001", "invalid model", "Model.play(1,1)")], null));
        _before = Snapshot(_destination);
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_fail_validation() => _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_planning_diagnostics() => _output.Stderr.ShouldContain("PLAY0001");
    [Fact] void should_say_nothing_was_checked_or_written() => _output.Stderr.ShouldContain("Nothing was checked or written");
    [Fact] void should_not_claim_publication() => _output.Stderr.ShouldNotContain("Nothing was published");
    [Fact] void should_emit_no_receipt() => _output.Stdout.ShouldEqual(string.Empty);
    [Fact] void should_leave_every_file_and_directory_unchanged() => Snapshot(_destination).ShouldEqual(_before);
    [Fact] void should_not_recover() => _publication.DidNotReceive().Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_check_a_failed_plan() => _publication.DidNotReceive().Check(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_publish() => _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
}
