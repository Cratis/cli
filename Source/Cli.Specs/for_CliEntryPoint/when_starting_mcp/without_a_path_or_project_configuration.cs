// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliEntryPoint.when_starting_mcp;

public class without_a_path_or_project_configuration : given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke("screenplay", "mcp");

    [Fact] void should_create_the_fallback_model_folder() => Directory.Exists(Path.Combine(_project, "Screenplay")).ShouldBeTrue();
    [Fact] void should_serve_the_fallback_model_folder() => _runner.Received(1).Run(Path.Combine(AiProjectPaths.PhysicalRoot(_project), "Screenplay"), _input, _output);
    [Fact] void should_not_write_diagnostics() => _error.ToString().ShouldBeEmpty();
    [Fact] void should_return_success() => _exitCode.ShouldEqual(ExitCodes.Success);
}
