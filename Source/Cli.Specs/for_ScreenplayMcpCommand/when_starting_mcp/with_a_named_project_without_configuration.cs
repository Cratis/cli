// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpCommand.when_starting_mcp;

public class with_a_named_project_without_configuration : given.a_protocol_invocation
{
    void Establish() => Directory.CreateDirectory(Path.Combine(_project, "Source"));

    void Because() => _exitCode = Invoke(new() { ProjectRoot = _project });

    [Fact] void should_serve_the_projects_source_folder() => _runner.Received(1).Run(Path.Combine(AiProjectPaths.PhysicalRoot(_project), "Source"), _input, _output);
    [Fact] void should_return_success() => _exitCode.ShouldEqual(ExitCodes.Success);
}
