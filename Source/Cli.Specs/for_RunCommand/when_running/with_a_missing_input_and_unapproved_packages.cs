// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_missing_input_and_unapproved_packages : given.a_run_command
{
    int _result;

    async Task Because() => _result = await Run(Path.Combine(_folder, "missing.play"), "--packages", _tools);

    [Fact] void should_reject_the_input() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_input_before_checking_packages() => _error.ToString().ShouldContain("Pass a .play file or a folder");
    [Fact] void should_not_report_the_package_approval_error() => _error.ToString().Contains("--packages needs --package-set", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
}
