// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_ReindexConstraintsCommand.when_waiting;

public class and_the_jobs_query_fails : given.a_reindex_context
{
    void Establish() =>
        _jobs.AllJobs(Arg.Any<AllJobsRequest>(), Arg.Any<CallContext>()).Returns(
            new QueryResult<IEnumerable<JobSummaryResponse>> { IsAuthorized = false });

    Task Because() => Run("--wait");

    [Fact] void should_not_report_success() => _result.ShouldEqual(ExitCodes.Inconclusive);
    [Fact] void should_say_access_was_denied() => _error.ToString().ShouldContain("denied");
    [Fact] void should_not_resubmit_the_reindex() => _constraintIndexes.ReceivedCalls().Count().ShouldEqual(1);
}
