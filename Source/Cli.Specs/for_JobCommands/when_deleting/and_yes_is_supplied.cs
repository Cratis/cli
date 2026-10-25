// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_JobCommands.when_deleting;

public class and_yes_is_supplied : given.a_delete_job_command
{
    int _result;

    void Establish()
    {
        _settings.Yes = true;
        _settings.Output = OutputFormats.Json;
        _settings.Server = "not-a-server";
    }

    async Task Because() => _result = await _command.ExecuteBeforeConnecting(_settings);

    [Fact] void should_pass_confirmation_and_attempt_connection_resolution() => _error.ToString().ShouldContain("Invalid Chronicle server connection string");
    [Fact] void should_report_the_invalid_server() => _result.ShouldEqual(ExitCodes.ValidationError);
}
