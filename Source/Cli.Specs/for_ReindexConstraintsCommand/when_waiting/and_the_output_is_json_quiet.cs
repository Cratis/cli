// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.for_ReindexConstraintsCommand.when_waiting;

public class and_the_output_is_json_quiet : given.a_reindex_context
{
    void Establish() => JobsAre(JobWith(JobStatus.CompletedSuccessfully));

    Task Because() => Run("-o", "json-quiet", "--wait");

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_write_exactly_one_json_document() => _output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Count(_ => _.TrimStart().StartsWith('{')).ShouldEqual(1);
}
