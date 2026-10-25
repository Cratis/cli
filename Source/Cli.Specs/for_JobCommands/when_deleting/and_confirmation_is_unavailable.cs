// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_JobCommands.when_deleting;

public class and_confirmation_is_unavailable : given.a_delete_job_command
{
    string? _previousNonInteractive;
    int _result;

    void Establish()
    {
        _previousNonInteractive = Environment.GetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable);
        Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, "1");
        _settings.Output = OutputFormats.Json;
        _settings.Server = "not-a-server";
    }

    async Task Because() => _result = await _command.ExecuteBeforeConnecting(_settings);

    void Destroy() => Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, _previousNonInteractive);

    [Fact] void should_report_a_validation_error_before_connecting() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_require_explicit_confirmation() => _error.ToString().ShouldContain("--yes");
}
