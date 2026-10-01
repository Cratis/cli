// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_event_sequence_cannot_be_checked : given.healthy_services
{
    void Establish() => _services.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(
        QueryResult<EventSequenceTailResponse>.Error(Guid.Empty, new Exception("Event sequence query failed")));

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_be_unhealthy() => _data.IsHealthy.ShouldBeFalse();
    [Fact] void should_name_the_check() => _data.ChecksCouldNotRun.Single().Check.ShouldEqual("Event sequence");
    [Fact] void should_preserve_the_reason() => _data.ChecksCouldNotRun.Single().Reason.ShouldContain("Event sequence query failed");
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
