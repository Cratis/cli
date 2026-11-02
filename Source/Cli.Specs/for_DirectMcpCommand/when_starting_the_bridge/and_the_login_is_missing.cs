// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand.when_starting_the_bridge;

public class and_the_login_is_missing : given.a_protocol_invocation
{
    void Establish() => _runner.Run(default!, default!, default!, default!, default).ReturnsForAnyArgs(_ => throw new DirectAuthError("Not logged in to Direct. Run 'cratis direct login'."));

    async Task Because() => _exitCode = await Invoke(new());

    [Fact] void should_write_nothing_to_the_protocol_stream() => _output.ToString().ShouldBeEmpty();
    [Fact] void should_explain_on_standard_error() => _error.ToString().ShouldContain("cratis direct login");
    [Fact] void should_fail_authentication() => _exitCode.ShouldEqual(ExitCodes.AuthenticationError);
}
