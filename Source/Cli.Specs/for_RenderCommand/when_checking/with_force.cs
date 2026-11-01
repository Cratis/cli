// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_force : given.a_check_command
{
    (int ExitCode, string Stdout, string Stderr) _output;

    void Establish()
    {
        _settings.Force = true;
        _command = new RenderCommand(_planning, _publication);
        _publication.Check(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>()).Returns(new ArtifactPublicationCheckResult(
            1, 0, 0, 0, false, new([new("managed.cs", "write", "before", "after")], new(null, "manifest"))));
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_report_pending_changes() => _output.ExitCode.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_plan_with_the_same_inputs() => _planning.Received(1).Plan(Arg.Is<ScreenplayRenderRequest>(_ => _.SourcePath == _folder && _.ApplicationName == "MyApp" && _.Target == "cratis"), Arg.Any<CancellationToken>());
    [Fact] void should_check_the_exact_plan_destination_and_force_choice() => _publication.Received(1).Check(Arg.Is<ArtifactPublicationRequest>(_ => ReferenceEquals(_.Plan, _artifactPlan) && _.Destination == _destination && _.Force), Arg.Any<CancellationToken>());
    [Fact] void should_not_recover() => _publication.DidNotReceive().Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_publish() => _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
}
