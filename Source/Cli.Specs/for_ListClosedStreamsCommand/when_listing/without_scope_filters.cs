// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Cli.for_ListClosedStreamsCommand.when_listing;

public class without_scope_filters : given.a_list_closed_streams_command
{
    JsonElement _row;

    async Task Because()
    {
        await Execute();
        _row = JsonSerializer.Deserialize<JsonElement>(_output.ToString())[0];
    }

    [Fact] void should_leave_unspecified_dimensions_unset() => _sequences.Received(1).ClosedStreams(Arg.Is<ClosedStreamsRequest>(_ => _.EventSourceId == null && _.EventSourceType == null && _.EventStreamType == null && _.EventStreamId == null), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_include_the_source_identifier() => _row.GetProperty("eventSourceId").GetString().ShouldEqual("order-42");
    [Fact] void should_include_the_source_type() => _row.GetProperty("eventSourceType").GetString().ShouldEqual("Order");
    [Fact] void should_include_the_stream_type() => _row.GetProperty("eventStreamType").GetString().ShouldEqual("Accounting");
    [Fact] void should_include_the_stream_identifier() => _row.GetProperty("eventStreamId").GetString().ShouldEqual("2026");
    [Fact] void should_include_the_origin_name() => _row.GetProperty("origin").GetString().ShouldEqual("ClosingEvent");
    [Fact] void should_include_the_owning_constraint() => _row.GetProperty("closedBy").GetString().ShouldEqual("CloseBooks");
    [Fact] void should_include_the_sequence_number() => _row.GetProperty("sequenceNumber").GetUInt64().ShouldEqual(123UL);
    [Fact] void should_include_the_closing_time_as_a_string() => _row.GetProperty("closedAt").GetString().ShouldContain("2026-10-10T12:00:00");
}
