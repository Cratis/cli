// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Spectre.Console;

namespace Cratis.Cli.for_ReplayObserverCommand.when_replaying_a_subscription_observer;

[Collection(CliSpecsCollection.Name)]
public class and_the_operator_declines : given.a_replay_observer_command
{
    static readonly string[] AgentVariables = ["CLAUDECODE", "CLAUDE_CODE_ENTRYPOINT", "CURSOR_TRACE_DIR", "TERM_PROGRAM", "WINDSURF_SESSION_ID", "PI_CODING_AGENT", "PI_SESSION_ID", AiAgentEnvironment.NonInteractiveEnvironmentVariable];
    Dictionary<string, string?> _previousEnvironment;
    IAnsiConsole _previousConsole;
    IAnsiConsole _console;
    IAnsiConsoleInput _input;
    TextWriter _previousOutput;
    StringWriter _output;

    void Establish()
    {
        _previousEnvironment = AgentVariables.ToDictionary(_ => _, Environment.GetEnvironmentVariable);
        foreach (var variable in AgentVariables)
        {
            Environment.SetEnvironmentVariable(variable, null);
        }

        _previousConsole = AnsiConsole.Console;
        _previousOutput = Console.Out;
        _output = new StringWriter();
        Console.SetOut(_output);
        var output = Substitute.For<IAnsiConsoleOutput>();
        output.IsTerminal.Returns(true);
        output.Width.Returns(120);
        output.Height.Returns(40);
        _console = Substitute.For<IAnsiConsole>();
        _console.Profile.Returns(new Profile(output, new Capabilities { Interactive = true }, System.Text.Encoding.UTF8));

        // Spectre runs the prompt inside this callback; a substitute otherwise returns the default answer without reading input.
        _console.ExclusivityMode.RunAsync(Arg.Any<Func<Task<char>>>())
            .Returns(call => call.Arg<Func<Task<char>>>()());
        _input = _console.Input;
        _input.ReadKeyAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ConsoleKeyInfo('n', ConsoleKey.N, false, false, false), new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        AnsiConsole.Console = _console;
        _settings.ObserverId = SubscriptionObserverId;
        _settings.EventSequenceId = "outbox";
        _settings.Server = "not-a-server";
    }

    async Task Because() => _result = await _command.ExecuteFromCommandLine(_settings);

    /// <inheritdoc/>
    protected override void CleanUp()
    {
        try
        {
            AnsiConsole.Console = _previousConsole;
            Console.SetOut(_previousOutput);
            _output.Dispose();
            foreach (var variable in _previousEnvironment)
            {
                Environment.SetEnvironmentVariable(variable.Key, variable.Value);
            }
        }
        finally
        {
            base.CleanUp();
        }
    }

    [Fact] void should_request_an_interactive_answer() => _input.Received(2).ReadKeyAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>());
    [Fact] void should_display_the_confirmation_prompt() => _console.Received(1).Write(Arg.Any<Markup>());
    [Fact] void should_succeed_without_resolving_the_invalid_server() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_that_the_replay_was_aborted() => _output.ToString().ShouldContain("Aborted.");
    [Fact] void should_not_replay_the_observer() => _observers.DidNotReceive().Replay(Arg.Any<Replay>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
}
