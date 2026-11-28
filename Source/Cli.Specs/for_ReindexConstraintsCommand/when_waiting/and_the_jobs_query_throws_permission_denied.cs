// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Queries;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_ReindexConstraintsCommand.when_waiting;

public class and_the_jobs_query_throws_permission_denied : given.a_reindex_context
{
    void Establish() =>
        _jobs.AllJobs(Arg.Any<AllJobsRequest>(), Arg.Any<CallContext>())
            .Returns<Task<QueryResult<IEnumerable<JobSummaryResponse>>>>(_ => throw new RpcException(new Status(StatusCode.PermissionDenied, "nope")));

    Task Because() => Run("--wait");

    [Fact] void should_be_inconclusive() => _result.ShouldEqual(ExitCodes.Inconclusive);
    [Fact] void should_name_the_job() => _error.ToString().ShouldContain(_jobId.ToString());
    [Fact] void should_say_it_was_not_resubmitted() => _error.ToString().ShouldContain("not resubmitted");
    [Fact] void should_carry_the_detail() => _error.ToString().ShouldContain("nope");
    [Fact] void should_not_resubmit_the_reindex() => _constraintIndexes.ReceivedCalls().Count().ShouldEqual(1);
}
