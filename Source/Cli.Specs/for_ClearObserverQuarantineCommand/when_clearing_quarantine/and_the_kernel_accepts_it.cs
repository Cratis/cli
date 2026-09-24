// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.Commands.Chronicle.Observers;
using ClearObserverQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearObserverQuarantine;

namespace Cratis.Cli.for_ClearObserverQuarantineCommand.when_clearing_quarantine;

/// <summary>
/// This replaces a specification that passed for the whole life of issue #185, while the command failed against
/// every kernel it was pointed at. The command resolved its contract method by reflection, which cannot see
/// explicitly implemented members, and the shipped client implements the contract explicitly - but the double it
/// was specified against implemented it implicitly, so the lookup succeeded in the specification and nowhere else.
/// Running against <see cref="given.an_observers_client_shaped_like_the_generated_proxy"/> is what closes that gap:
/// the double now fails the way the real client does.
/// </summary>
public class and_the_kernel_accepts_it : Specification
{
    IObservers _observers;
    IServices _services;
    ObserverCommandSettings _settings;
    int _result;
    ClearObserverQuarantineContract _sent;

    void Establish()
    {
        _observers = Substitute.For<IObservers>();
        _services = Substitute.For<IServices>();
        _services.Observers.Returns(new given.an_observers_client_shaped_like_the_generated_proxy(_observers));

        _settings = new ObserverCommandSettings
        {
            EventStore = "the-event-store",
            Namespace = "the-namespace",
            ObserverId = "the-observer",
            EventSequenceId = "event-log"
        };
    }

    async Task Because()
    {
        _result = await new ClearObserverQuarantineCommandForSpecs().Execute(_services, _settings);
        _sent = _observers.ReceivedCalls()
            .Select(call => call.GetArguments()[0])
            .OfType<ClearObserverQuarantineContract>()
            .Single();
    }

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_clear_the_observer_it_was_given() => _sent.ObserverId.ShouldEqual("the-observer");
    [Fact] void should_target_the_resolved_event_store() => _sent.EventStore.ShouldEqual("the-event-store");
    [Fact] void should_target_the_resolved_namespace() => _sent.Namespace.ShouldEqual("the-namespace");
    [Fact] void should_pass_the_event_sequence() => _sent.EventSequenceId.ShouldEqual("event-log");

    class ClearObserverQuarantineCommandForSpecs : ClearObserverQuarantineCommand
    {
        public Task<int> Execute(IServices services, ObserverCommandSettings settings) =>
            ExecuteCommandAsync(services, settings, "json");
    }
}
