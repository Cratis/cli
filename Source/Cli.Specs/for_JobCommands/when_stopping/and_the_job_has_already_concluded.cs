// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.for_JobCommands.when_stopping;

public class and_the_job_has_already_concluded : given.a_job_command_context
{
    int _result;

    void Establish() => StopAnswers(false, JobCommandRefusal.JobIsCompleted);

    async Task Because() => _result = await Stop();

    [Fact] void should_not_report_success() => _result.ShouldNotEqual(ExitCodes.Success);
    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_the_job_has_concluded() => _error.ToString().ShouldContain("already concluded");
    [Fact] void should_not_claim_the_job_was_stopped() => _output.ToString().ShouldNotContain("stopped successfully");
}
