// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.Observers;

namespace Cratis.Cli.for_ReplayObserverCommand;

public class when_checking_for_a_subscription_replay : Specification
{
    bool _forwardingIdentity;
    bool _outboxSequence;
    bool _ordinaryObserver;

    void Because()
    {
        _forwardingIdentity = ReplayObserverCommand.IsSubscriptionReplay("orders-subscription->consumer-store", "event-log");
        _outboxSequence = ReplayObserverCommand.IsSubscriptionReplay("orders-forwarder", "outbox");
        _ordinaryObserver = ReplayObserverCommand.IsSubscriptionReplay("ordinary-observer", "event-log");
    }

    [Fact] void should_detect_a_forwarding_observer_identity() => _forwardingIdentity.ShouldBeTrue();
    [Fact] void should_detect_an_outbox_sequence() => _outboxSequence.ShouldBeTrue();
    [Fact] void should_not_detect_an_ordinary_observer() => _ordinaryObserver.ShouldBeFalse();
}
