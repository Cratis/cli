// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using RemoveObserverContract = Cratis.Chronicle.Contracts.Observation.RemoveObserver;

namespace Cratis.Cli.for_RemoveObserverCommand.when_removing;

public class and_the_observer_is_removed : given.a_remove_observer_command
{
    int _result;
    RemoveObserverContract _sent;

    async Task Because()
    {
        _result = await Execute();
        _sent = _observers.ReceivedCalls()
            .Select(call => call.GetArguments()[0])
            .OfType<RemoveObserverContract>()
            .Single();
    }

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_remove_the_observer_it_was_given() => _sent.ObserverId.ShouldEqual("the-observer");
    [Fact] void should_target_the_resolved_event_store() => _sent.EventStore.ShouldEqual("the-event-store");
    [Fact] void should_target_the_resolved_namespace() => _sent.Namespace.ShouldEqual("the-namespace");
    [Fact] void should_pass_the_event_sequence() => _sent.EventSequenceId.ShouldEqual("event-log");
}
