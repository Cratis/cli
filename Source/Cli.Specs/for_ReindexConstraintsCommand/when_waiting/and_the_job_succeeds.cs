// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.for_ReindexConstraintsCommand.when_waiting;

public class and_the_job_succeeds : given.a_reindex_context
{
    void Establish() => JobsAre(JobWith(JobStatus.CompletedSuccessfully));

    Task Because() => Run("--wait");

    [Fact] void should_return_the_expected_exit_code() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_start_exactly_one_reindex() => _constraintIndexes.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_write_one_json_document_with_the_job_id() => _output.ToString().ShouldContain(_jobId.ToString());
    [Fact] void should_report_the_final_status() => _output.ToString().ShouldContain("CompletedSuccessfully");
}
