// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.Commands.Chronicle.Observers;

namespace Cratis.Cli.for_ObserverCommandSettings.given;

public class observer_definitions : Specification
{
    protected IObservers _observers;
    protected ObserverCommandSettings _settings;
    protected string _result;

    void Establish()
    {
        _observers = Substitute.For<IObservers>();
        _settings = new ObserverCommandSettings
        {
            EventStore = "the-event-store",
            Namespace = "the-namespace",
            ObserverId = "system-observer"
        };
        _observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(
        [
            new ObserverInformation { Id = "system-observer", EventSequenceId = "system" },
            new ObserverInformation { Id = "ordinary-observer", EventSequenceId = "event-log" }
        ]);
    }

    protected async Task Resolve() => _result = await _settings.ResolveEventSequenceId(_observers);
}
