// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_a_baseline_is_given_with_a_workspace : given.a_baseline_workspace
{
    void Establish() => File.WriteAllText(_implicitPath, "malformed implicit baseline");
    async Task Because() => _output = await Capture();

    [Fact] void should_publish_successfully() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_use_the_explicit_baseline_instead_of_the_discovered_file() => _requests.Single().ReleaseBaseline.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Checked);
    [Fact] void should_report_the_explicit_baseline_violation() => _output.Stderr.ShouldContain("STAGE-BASELINE-001");
    [Fact] void should_not_report_the_implicit_baseline_error() => _output.Stderr.ShouldNotContain("STAGE-BASELINE-003");
}
