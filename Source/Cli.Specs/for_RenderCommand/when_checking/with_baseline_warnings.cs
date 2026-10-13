// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_baseline_warnings : given.a_baseline_workspace
{
    (int ExitCode, string Stdout, string Stderr) _pending;

    async Task Because()
    {
        _settings.Check = true;
        _pending = await Capture();
        _settings.Check = false;
        (await Capture()).ExitCode.ShouldEqual(ExitCodes.Success);
        _publication.ClearReceivedCalls();
        _settings.Check = true;
        _output = await Capture();
    }

    [Fact] void should_keep_changes_pending_exit_for_a_fresh_destination() => _pending.ExitCode.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_keep_success_exit_for_a_current_destination() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_the_warning_for_changes_pending() => _pending.Stderr.ShouldContain("STAGE-BASELINE-001");
    [Fact] void should_report_the_warning_for_an_unchanged_destination() => _output.Stderr.ShouldContain("STAGE-BASELINE-001");
    [Fact] async Task should_not_recover_in_check_mode() => await _publication.DidNotReceive().Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_not_publish_in_check_mode() => await _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
}
