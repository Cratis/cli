// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliEntryPoint.when_starting_mcp;

public class without_a_host_project_anchor : given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke("screenplay", "mcp", "--project-root-env", "CLAUDE_PROJECT_DIR");

    [Fact] void should_not_fall_back_to_a_different_model() => _runner.DidNotReceiveWithAnyArgs().Run(default!, default!, default!);
    [Fact] void should_name_the_missing_host_variable() => _error.ToString().ShouldContain("CLAUDE_PROJECT_DIR");
    [Fact] void should_return_nonzero() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
