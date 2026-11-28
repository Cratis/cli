// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ReindexConstraintsCommand.when_waiting;

public class and_the_job_is_missing : given.a_reindex_context
{
    void Establish() => JobsAre();

    Task Because() => Run("--wait");

    [Fact] void should_not_report_success() => _result.ShouldNotEqual(ExitCodes.Success);
    [Fact] void should_report_it_as_inconclusive() => _result.ShouldEqual(ExitCodes.Inconclusive);
    [Fact] void should_name_the_job() => _error.ToString().ShouldContain(_jobId.ToString());
    [Fact] void should_not_resubmit_the_reindex() => _constraintIndexes.ReceivedCalls().Count().ShouldEqual(1);
}
