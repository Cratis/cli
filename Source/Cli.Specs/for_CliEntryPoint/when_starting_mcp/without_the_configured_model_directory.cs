// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliEntryPoint.when_starting_mcp;

public class without_the_configured_model_directory : given.a_protocol_invocation
{
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_project, ".cratis"));
        File.WriteAllText(Path.Combine(_project, ".cratis/ai.json"), """{"mcpServers":{"screenplay":{"enabled":true,"root":"models"}}}""");
    }

    async Task Because() => _exitCode = await Invoke("screenplay", "mcp", "--project-root", _project);

    [Fact] void should_require_setup_instead_of_creating_source_directories_at_startup() => Directory.Exists(Path.Combine(_project, "models")).ShouldBeFalse();
    [Fact] void should_not_run_the_wrong_model() => _runner.DidNotReceiveWithAnyArgs().Run(default!, default!, default!);
    [Fact] void should_name_the_missing_directory() => _error.ToString().ShouldContain("models");
    [Fact] void should_return_nonzero() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
