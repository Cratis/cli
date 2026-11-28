// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliApp;

public class when_requesting_constraints_help : given.a_cli_process
{
    async Task Because() => await Run("chronicle", "constraints", "--help");

    [Fact] void should_list_reindex() => _output.ShouldContain("reindex");
    [Fact] void should_exit_successfully() => _exitCode.ShouldEqual(ExitCodes.Success);
}
