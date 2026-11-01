// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_executing_uninstall : given.a_desktop_command
{
    async Task Because() => _exitCode = await new UninstallDesktopMcpCommand(_run, _output, _error).ExecuteAsync(_context, new() { Clients = "chatgpt", Version = "4.55.0", DryRun = true }, CancellationToken.None);

    [Fact] void should_pass_the_bound_removal_settings() => _run.Received(1)("uninstall", "chatgpt", "4.55.0", null, true, _output, _error);
    [Fact] void should_preserve_the_operation_exit_code() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
