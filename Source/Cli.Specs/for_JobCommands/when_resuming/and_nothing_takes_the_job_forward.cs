// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.for_JobCommands.when_resuming;

public class and_nothing_takes_the_job_forward : given.a_job_command_context
{
    int _result;

    void Establish() => ResumeAnswers(false, JobCommandRefusal.NotResumed);

    async Task Because() => _result = await Resume();

    [Fact] void should_not_report_success() => _result.ShouldNotEqual(ExitCodes.Success);
    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_nothing_took_the_job_forward() => _error.ToString().ShouldContain("Nothing took job");
    [Fact] void should_not_claim_the_job_was_resumed() => _output.ToString().ShouldNotContain("resumed successfully");
}
