// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Cli.given;

namespace Cratis.Cli.for_ChronicleCommand.when_connecting;

[Collection(CliSpecsCollection.Name)]
public class and_the_server_is_malformed : a_temp_config_directory
{
    int _result;
    StringWriter _error = null!;
    TextWriter _previousError = null!;
    bool _commandExecuted;

    void Establish()
    {
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    async Task Because()
    {
        _result = await ((ICommand<ChronicleSettings>)new TestCommand(() => _commandExecuted = true)).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "test", null),
            new ChronicleSettings { Server = "not-a-chronicle-url", Output = OutputFormats.JsonCompact },
            CancellationToken.None);
    }

    /// <inheritdoc/>
    protected override void CleanUp()
    {
        try
        {
            Console.SetError(_previousError);
        }
        finally
        {
            base.CleanUp();
        }
    }

    [Fact] void should_return_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_emit_a_json_error() => _error.ToString().ShouldContain($"\"error\":\"{ExitCodes.ValidationErrorCode}\"");
    [Fact] void should_not_execute_the_command() => _commandExecuted.ShouldBeFalse();

    sealed class TestCommand(Action executed) : ChronicleCommand<ChronicleSettings>
    {
        protected override Task<int> ExecuteCommandAsync(IServices services, ChronicleSettings settings, string format)
        {
            executed();
            return Task.FromResult(ExitCodes.Success);
        }
    }
}
