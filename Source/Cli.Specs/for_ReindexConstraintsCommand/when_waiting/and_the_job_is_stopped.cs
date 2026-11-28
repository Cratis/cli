// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.for_ReindexConstraintsCommand.when_waiting;

public class and_the_job_is_stopped : given.a_reindex_context
{
    void Establish() => JobsAre(JobWith(JobStatus.Stopped));

    Task Because() => Run("--wait");

    [Fact] void should_return_the_expected_exit_code() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_start_exactly_one_reindex() => _constraintIndexes.ReceivedCalls().Count().ShouldEqual(1);
}
