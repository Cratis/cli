// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliEntryPoint.when_starting_mcp;

public class with_a_valid_invocation : given.a_protocol_invocation
{
    const string Packet = "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{}}\n";

    void Establish() => _runner.When(runner => runner.Run(Arg.Any<string>(), _input, _output)).Do(_ => _output.Write(Packet));
    async Task Because() => _exitCode = await Invoke("screenplay", "mcp", _project);

    [Fact] void should_emit_only_the_protocol_packet() => _output.ToString().ShouldEqual(Packet);
    [Fact] void should_not_initialize_the_cli_that_starts_update_network_requests() => _interactiveStarted.ShouldBeFalse();
    [Fact] void should_pass_the_streams_and_physical_root_to_the_embedded_runner() => _runner.Received(1).Run(AiProjectPaths.PhysicalRoot(_project), _input, _output);
    [Fact] void should_not_write_diagnostics_on_success() => _error.ToString().ShouldBeEmpty();
    [Fact] void should_return_success_after_eof() => _exitCode.ShouldEqual(ExitCodes.Success);
}
