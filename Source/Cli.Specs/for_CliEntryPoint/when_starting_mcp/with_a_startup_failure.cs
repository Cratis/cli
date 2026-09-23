// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliEntryPoint.when_starting_mcp;

public class with_a_startup_failure : given.a_protocol_invocation
{
    void Establish() => _runner.When(runner => runner.Run(Arg.Any<string>(), _input, _output)).Do(_ => throw new AiMcpConfigurationInvalid("cannot start"));
    async Task Because() => _exitCode = await Invoke("screenplay", "mcp");

    [Fact] void should_not_pollute_stdout() => _output.ToString().ShouldBeEmpty();
    [Fact] void should_report_failure_on_stderr() => _error.ToString().ShouldContain("cannot start");
    [Fact] void should_return_nonzero() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
