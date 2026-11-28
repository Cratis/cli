// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.for_ReindexConstraintsCommand.when_waiting;

public class and_the_job_never_finishes : given.a_reindex_context
{
    void Establish()
    {
        ReindexConstraintsCommandForSpecs.Timeout = TimeSpan.FromMilliseconds(50);
        JobsAre(JobWith(JobStatus.Running));
    }

    Task Because() => Run("--wait");

    [Fact] void should_be_inconclusive() => _result.ShouldEqual(ExitCodes.Inconclusive);
    [Fact] void should_name_the_job() => _error.ToString().ShouldContain(_jobId.ToString());
    [Fact] void should_not_resubmit_the_reindex() => _constraintIndexes.ReceivedCalls().Count().ShouldEqual(1);
}
