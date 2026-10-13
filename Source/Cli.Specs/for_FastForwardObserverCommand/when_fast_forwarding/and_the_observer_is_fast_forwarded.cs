// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using FastForwardContract = Cratis.Chronicle.Contracts.Observation.FastForward;

namespace Cratis.Cli.for_FastForwardObserverCommand.when_fast_forwarding;

public class and_the_observer_is_fast_forwarded : given.a_fast_forward_observer_command
{
    int _result;

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_send_the_observer_and_scope() => _observers.Received(1).FastForward(
        Arg.Is<FastForwardContract>(_ => _.EventStore == "the-event-store" && _.Namespace == "the-namespace" && _.ObserverId == "the-observer" && _.EventSequenceId == "event-log"),
        Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_report_where_it_went() => _output.ToString().ShouldContain("101");
}
