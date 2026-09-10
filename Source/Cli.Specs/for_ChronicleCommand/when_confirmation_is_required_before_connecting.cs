// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Cli.for_ChronicleCommand;

[Collection(CliSpecsCollection.Name)]
public class when_confirmation_is_required_before_connecting : Specification
{
    string? _previousNonInteractive;
    ConfirmationCommand _command;
    int _result;

    void Establish()
    {
        _previousNonInteractive = Environment.GetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable);
        Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, "1");
        _command = new ConfirmationCommand();
    }

    async Task Because() =>
        _result = await _command.Execute(new ChronicleSettings
        {
            Output = OutputFormats.JsonCompact,
            Server = "chronicle://127.0.0.1:1"
        });

    void Destroy() =>
        Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, _previousNonInteractive);

    [Fact] void should_return_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_invoke_the_connected_command() => _command.CommandWasExecuted.ShouldBeFalse();

    sealed class ConfirmationCommand : ChronicleCommand<ChronicleSettings>
    {
        public bool CommandWasExecuted { get; private set; }

        public Task<int> Execute(ChronicleSettings settings) =>
            ExecuteAsync(null!, settings, CancellationToken.None);

        protected override string GetConfirmationPrompt(ChronicleSettings settings) => "Confirm destructive operation?";

        protected override Task<int> ExecuteCommandAsync(IServices services, ChronicleSettings settings, string format)
        {
            CommandWasExecuted = true;
            return Task.FromResult(ExitCodes.Success);
        }
    }
}
