// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Context;

namespace Cratis.Cli.for_DeleteContextCommand;

[Collection(CliSpecsCollection.Name)]
public class when_confirmation_is_unavailable : given.a_temp_config_directory
{
    string? _previousNonInteractive;
    TextWriter _previousError;
    string _configBefore;
    int _result;

    void Establish()
    {
        _previousNonInteractive = Environment.GetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable);
        Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, "1");

        var config = new CliConfiguration
        {
            ActiveContext = "keep",
            Contexts = new Dictionary<string, CliContext>
            {
                ["keep"] = new(),
                ["remove-me"] = new()
            }
        };
        config.Save();
        _configBefore = File.ReadAllText(CliConfiguration.GetConfigPath());
    }

    async Task Because()
    {
        _previousError = Console.Error;
        Console.SetError(new StringWriter());
        _result = await new TestDeleteContextCommand().Execute(new ContextNameSettings
        {
            Name = "remove-me",
            Output = OutputFormats.JsonCompact
        });
        Console.SetError(_previousError);
    }

    protected override void CleanUp()
    {
        Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, _previousNonInteractive);
        base.CleanUp();
    }

    [Fact] void should_return_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_leave_the_configuration_unchanged() => File.ReadAllText(CliConfiguration.GetConfigPath()).ShouldEqual(_configBefore);
    [Fact] void should_not_delete_the_context() => CliConfiguration.Load().Contexts.ContainsKey("remove-me").ShouldBeTrue();

    sealed class TestDeleteContextCommand : DeleteContextCommand
    {
        public Task<int> Execute(ContextNameSettings settings) =>
            ExecuteAsync(null!, settings, CancellationToken.None);
    }
}
