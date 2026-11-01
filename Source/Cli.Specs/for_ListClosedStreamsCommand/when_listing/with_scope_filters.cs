// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Cli.for_ListClosedStreamsCommand.when_listing;

public class with_scope_filters : given.a_list_closed_streams_command
{
    int _result;

    void Establish()
    {
        _settings.EventSourceId = "order-42";
        _settings.EventSourceType = "Order";
        _settings.EventStreamType = "Accounting";
        _settings.EventStreamId = "2026";
    }

    async Task Because() => _result = await Execute("plain");

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_send_all_scope_and_sequence_dimensions() => _sequences.Received(1).ClosedStreams(Arg.Is<ClosedStreamsRequest>(_ => _.EventStore == "store" && _.Namespace == "tenant" && _.EventSequenceId == "outbox" && _.EventSourceId == "order-42" && _.EventSourceType == "Order" && _.EventStreamType == "Accounting" && _.EventStreamId == "2026"), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_render_scope_origin_owner_position_and_time() => _output.ToString().ShouldContain("order-42\tOrder\tAccounting\t2026\tClosingEvent\tCloseBooks\t123\t2026-10-10T12:00:00");
}
