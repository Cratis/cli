// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_ReindexConstraintsCommand.when_waiting;

public class and_the_job_is_running_then_succeeds : given.a_reindex_context
{
    void Establish() =>
        _jobs.AllJobs(Arg.Any<AllJobsRequest>(), Arg.Any<CallContext>()).Returns(
            new QueryResult<IEnumerable<JobSummaryResponse>> { Data = [JobWith(JobStatus.Running)] },
            new QueryResult<IEnumerable<JobSummaryResponse>> { Data = [JobWith(JobStatus.CompletedSuccessfully)] });

    Task Because() => Run("--wait");

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_poll_again_after_running() => _jobs.ReceivedCalls().Count().ShouldEqual(2);
    [Fact] void should_start_exactly_one_reindex() => _constraintIndexes.ReceivedCalls().Count().ShouldEqual(1);
}
