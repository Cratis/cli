// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_JobCommands.when_deleting;

public class and_the_job_id_is_missing : given.a_delete_job_command
{
    int _result;

    void Establish()
    {
        _settings.JobId = string.Empty;
        _settings.Output = OutputFormats.Json;
        _settings.Server = "not-a-server";
        _settings.Yes = true;
    }

    async Task Because() => _result = await _command.ExecuteBeforeConnecting(_settings);

    [Fact] void should_report_a_validation_error_before_connecting() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_describe_the_missing_id() => _error.ToString().ShouldContain("must be a valid GUID");
}
