// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.Jobs;

namespace Cratis.Cli.for_JobCommands;

[Collection(CliSpecsCollection.Name)]
public class and_stopping_a_job_when_confirmation_is_unavailable : Specification
{
    string? _previousNonInteractive;
    TextWriter _previousError;
    int _result;

    void Establish()
    {
        _previousNonInteractive = Environment.GetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable);
        Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, "1");
    }

    async Task Because()
    {
        _previousError = Console.Error;
        Console.SetError(new StringWriter());
        _result = await new TestStopJobCommand().Execute(new JobCommandSettings
        {
            JobId = Guid.Empty.ToString(),
            Output = OutputFormats.JsonCompact,
            Server = "chronicle://127.0.0.1:1"
        });
        Console.SetError(_previousError);
    }

    void Destroy() =>
        Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, _previousNonInteractive);

    [Fact] void should_return_a_validation_error_before_connecting() => _result.ShouldEqual(ExitCodes.ValidationError);

    sealed class TestStopJobCommand : StopJobCommand
    {
        public Task<int> Execute(JobCommandSettings settings) =>
            ExecuteAsync(null!, settings, CancellationToken.None);
    }
}
