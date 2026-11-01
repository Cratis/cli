// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.for_JobCommands.when_stopping;

public class and_the_stop_could_not_be_persisted : given.a_job_command_context
{
    int _result;

    void Establish() => StopAnswers(false, JobCommandRefusal.CouldNotPersist);

    async Task Because() => _result = await Stop();

    [Fact] void should_report_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_say_the_job_was_left_as_it_was() => _error.ToString().ShouldContain("left as it was");
}
