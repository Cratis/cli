// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpCommand.when_starting_mcp;

public class with_workspace_state_in_several_folders : given.a_protocol_invocation
{
    void Establish()
    {
        foreach (var folder in new[] { _project, Path.Combine(_project, "Models") })
        {
            Directory.CreateDirectory(Path.Combine(folder, ".screenplay"));
            File.WriteAllText(Path.Combine(folder, ".screenplay", "identities.json"), "{}");
        }

        File.WriteAllText(Path.Combine(_project, "Models", "application.play"), "domain Sample\n");
    }

    void Because() => _exitCode = Invoke(new() { ProjectRoot = _project });

    [Fact] void should_start_the_server() => _exitCode.ShouldEqual(0);
    [Fact] void should_serve_the_folder_nearest_the_project() => _runner.Received(1).Run(Arg.Is<string?>(root => root == AiProjectPaths.PhysicalRoot(_project)), _input, _output);
    [Fact] void should_report_the_competing_folder_on_standard_error() => _error.ToString().ShouldContain(Path.Combine(AiProjectPaths.PhysicalRoot(_project), "Models"));
    [Fact] void should_keep_standard_output_for_the_protocol() => _output.ToString().ShouldBeEmpty();
}
