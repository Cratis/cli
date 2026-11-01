// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_executing_status : given.a_desktop_command
{
    async Task Because() => _exitCode = await new DesktopMcpStatusCommand(_run, _output, _error).ExecuteAsync(_context, new() { Clients = "claude", Version = "4.55.0" }, CancellationToken.None);

    [Fact] void should_pass_the_bound_inspection_settings() => _run.Received(1)("status", "claude", "4.55.0", null, false, _output, _error);
    [Fact] void should_preserve_the_operation_exit_code() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
