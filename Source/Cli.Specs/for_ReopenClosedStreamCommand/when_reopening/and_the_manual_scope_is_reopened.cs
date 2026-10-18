// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Cli.for_ReopenClosedStreamCommand.when_reopening;

public class and_the_manual_scope_is_reopened : given.a_reopen_closed_stream_command
{
    int _result;

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_send_the_exact_dimensions_and_reason() => _sequences.Received(1).ReopenStreamScope(Arg.Is<ReopenStreamScopeRequest>(_ => _.EventStore == "store" && _.Namespace == "tenant" && _.EventSequenceId == "outbox" && _.EventSourceId == "order-42" && _.EventSourceType == "Order" && _.EventStreamType == "Accounting" && _.EventStreamId == "2026" && _.Reason == "Closed the wrong order"), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_not_supply_an_actor_in_place_of_the_authenticated_principal() => _sequences.Received(1).ReopenStreamScope(Arg.Is<ReopenStreamScopeRequest>(_ => _.CausedBy == null), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_describe_the_repair_audit() => _output.ToString().ShouldContain("StreamScopeReopened");
    [Fact] void should_not_promise_that_appends_are_now_allowed() => _output.ToString().ShouldContain("Other covering closures may still prevent appends");
}
