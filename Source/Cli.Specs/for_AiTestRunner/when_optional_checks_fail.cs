// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_optional_checks_fail : given.a_conformance_run
{
    void Establish() => _fixture = _fixture with
    {
        Checks = [
        new() { Type = "file_exists", Path = "absent-before-harness", Phase = AiTestPhase.Install, Optional = true },
        new() { Type = "file_exists", Path = "result.txt" },
        new() { Type = "file_exists", Path = "absent-after-harness", Optional = true },
        new() { Type = "file_matches", Path = "result.txt", Pattern = "[", Optional = true },
        new() { Type = "command", Run = ["fake"], ExitCode = 1, Optional = true }
    ]
    };
    async Task Because() => _report = await Run();
    [Fact] void should_not_stop_before_harness_for_optional_install_failure() => _workspace.ShouldNotBeEmpty();
    [Fact] void should_report_optional_file_failure() => _report.Results.Single().Checks[2].Status.ShouldEqual("failed");
    [Fact] void should_report_optional_file_error() => _report.Results.Single().Checks[3].Status.ShouldEqual("error");
    [Fact] void should_report_optional_command_failure() => _report.Results.Single().Checks[4].Status.ShouldEqual("failed");
    [Fact] void should_preserve_optional_flag() => _report.Results.Single().Checks[3].Optional.ShouldBeTrue();
    [Fact] void should_not_fail_fixture_for_optional_checks() => _report.Results.Single().Status.ShouldEqual("passed");
    [Fact] void should_not_fail_exit_code_for_optional_checks() => _report.ExitCode.ShouldEqual(0);
}
