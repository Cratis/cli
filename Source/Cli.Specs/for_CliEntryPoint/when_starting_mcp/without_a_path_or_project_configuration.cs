// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliEntryPoint.when_starting_mcp;

public class without_a_path_or_project_configuration : given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke("screenplay", "mcp");

    [Fact] void should_let_the_server_choose_its_workspace() => _runner.Received(1).Run(null, _input, _output);
    [Fact] void should_not_create_a_folder_in_the_working_directory() => Directory.Exists(Path.Combine(_project, "Screenplay")).ShouldBeFalse();
    [Fact] void should_not_write_diagnostics() => _error.ToString().ShouldBeEmpty();
    [Fact] void should_return_success() => _exitCode.ShouldEqual(ExitCodes.Success);
}
