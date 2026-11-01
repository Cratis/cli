// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Cli.for_ListClosedStreamsCommand.when_listing;

public class and_legacy_manual_closure_metadata_is_absent : given.a_list_closed_streams_command
{
    int _result;

    void Establish() => _response.Data = [new() { EventSourceId = "order-42", Origin = ClosedStreamOrigin.CompleteStream, SequenceNumber = ulong.MaxValue }];

    async Task Because() => _result = await Execute("plain");

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_keep_missing_dimensions_owner_and_time_empty() => _output.ToString().ShouldContain("order-42\t\t\t\tCompleteStream\t\t18446744073709551615\t");
}
