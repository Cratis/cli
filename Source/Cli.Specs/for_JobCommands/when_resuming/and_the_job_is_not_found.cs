// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.for_JobCommands.when_resuming;

public class and_the_job_is_not_found : given.a_job_command_context
{
    int _result;

    void Establish() => ResumeAnswers(false, JobCommandRefusal.NotFound);

    async Task Because() => _result = await Resume();

    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_the_job_was_not_found() => _error.ToString().ShouldContain("was not found");
}
