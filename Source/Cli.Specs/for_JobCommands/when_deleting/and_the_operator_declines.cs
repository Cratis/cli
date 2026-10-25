// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console;

namespace Cratis.Cli.for_JobCommands.when_deleting;

public class and_the_operator_declines : given.a_delete_job_command
{
    static readonly string[] AgentVariables = ["CLAUDECODE", "CLAUDE_CODE_ENTRYPOINT", "CURSOR_TRACE_DIR", "TERM_PROGRAM", "WINDSURF_SESSION_ID", "PI_CODING_AGENT", "PI_SESSION_ID", AiAgentEnvironment.NonInteractiveEnvironmentVariable];
    Dictionary<string, string?> _previousEnvironment;
    IAnsiConsole _previousConsole;
    int _result;

    void Establish()
    {
        _previousEnvironment = AgentVariables.ToDictionary(_ => _, Environment.GetEnvironmentVariable);
        foreach (var variable in AgentVariables)
        {
            Environment.SetEnvironmentVariable(variable, null);
        }

        _previousConsole = AnsiConsole.Console;
        var output = Substitute.For<IAnsiConsoleOutput>();
        output.IsTerminal.Returns(true);
        output.Width.Returns(120);
        output.Height.Returns(40);
        var console = Substitute.For<IAnsiConsole>();
        console.Profile.Returns(new Profile(output, new Capabilities { Interactive = true }, System.Text.Encoding.UTF8));
        console.Input.ReadKeyAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        AnsiConsole.Console = console;
        _settings.Output = OutputFormats.Json;
        _settings.Server = "not-a-server";
    }

    async Task Because() => _result = await _command.ExecuteBeforeConnecting(_settings);

    void Destroy()
    {
        AnsiConsole.Console = _previousConsole;
        foreach (var variable in _previousEnvironment)
        {
            Environment.SetEnvironmentVariable(variable.Key, variable.Value);
        }
    }

    [Fact] void should_report_a_validation_error_before_connecting() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_default_to_no_and_abort() => _output.ToString().ShouldContain("Aborted.");
    [Fact] void should_not_attempt_to_resolve_the_server() => _error.ToString().ShouldBeEmpty();
}
