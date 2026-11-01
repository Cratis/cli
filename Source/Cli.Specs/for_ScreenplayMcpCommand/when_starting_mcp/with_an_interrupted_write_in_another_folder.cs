// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpCommand.when_starting_mcp;

public class with_an_interrupted_write_in_another_folder : given.a_protocol_invocation
{
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_project, ".screenplay"));
        File.WriteAllText(Path.Combine(_project, ".screenplay", "identities.json"), "{}");
        Directory.CreateDirectory(Path.Combine(_project, "Models", ".screenplay"));
        File.WriteAllText(Path.Combine(_project, "Models", ".screenplay", "pending.json"), "{}");
        File.WriteAllText(Path.Combine(_project, "Models", "application.play"), "domain Sample\n");
    }

    void Because() => _exitCode = Invoke(new() { ProjectRoot = _project });

    [Fact] void should_fail_with_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_start_the_server() => _runner.DidNotReceive().Run(Arg.Any<string?>(), Arg.Any<TextReader>(), Arg.Any<TextWriter>());
    [Fact] void should_name_the_interrupted_write_on_standard_error() => _error.ToString().ShouldContain("PendingOperation");
    [Fact] void should_keep_standard_output_for_the_protocol() => _output.ToString().ShouldBeEmpty();
}
