// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_ReindexConstraintsCommand.when_waiting;

public class and_the_user_cancels : given.a_reindex_context
{
    void Establish()
    {
        var source = new CancellationTokenSource();
        ReindexConstraintsCommandForSpecs.Token = source.Token;
        _jobs.AllJobs(Arg.Any<AllJobsRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            source.Cancel();
            return new QueryResult<IEnumerable<JobSummaryResponse>> { Data = [JobWith(JobStatus.Running)] };
        });
    }

    Task Because() => Run("--wait");

    [Fact] void should_be_inconclusive() => _result.ShouldEqual(ExitCodes.Inconclusive);
    [Fact] void should_name_the_job() => _error.ToString().ShouldContain(_jobId.ToString());
    [Fact] void should_say_the_job_keeps_running() => _error.ToString().ShouldContain("keeps running");
}
