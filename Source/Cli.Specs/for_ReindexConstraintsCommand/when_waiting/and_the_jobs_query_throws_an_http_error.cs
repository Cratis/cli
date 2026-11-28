// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_ReindexConstraintsCommand.when_waiting;

public class and_the_jobs_query_throws_an_http_error : given.a_reindex_context
{
    void Establish() =>
        _jobs.AllJobs(Arg.Any<AllJobsRequest>(), Arg.Any<CallContext>())
            .Returns<Task<QueryResult<IEnumerable<JobSummaryResponse>>>>(_ => throw new HttpRequestException("handshake failed"));

    Task Because() => Run("--wait");

    [Fact] void should_be_inconclusive() => _result.ShouldEqual(ExitCodes.Inconclusive);
    [Fact] void should_name_the_job() => _error.ToString().ShouldContain(_jobId.ToString());
    [Fact] void should_carry_the_detail() => _error.ToString().ShouldContain("handshake failed");
}
