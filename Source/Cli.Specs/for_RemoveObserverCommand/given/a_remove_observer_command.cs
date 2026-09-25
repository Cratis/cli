// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.Commands.Chronicle.Observers;
using Cratis.Cli.given;
using RemoveObserverContract = Cratis.Chronicle.Contracts.Observation.RemoveObserver;

namespace Cratis.Cli.for_RemoveObserverCommand.given;

public class a_remove_observer_command : Specification
{
    protected IServices _services;
    protected IObservers _observers;
    protected ObserverCommandSettings _settings;
    protected RemoveObserverCommandForSpecs _command;

    void Establish()
    {
        _observers = Substitute.For<IObservers>();
        _services = Substitute.For<IServices>();

        // Explicitly implemented, like the shipped client - see the double's own remarks and issue #185.
        _services.Observers.Returns(new an_observers_client_shaped_like_the_generated_proxy(_observers));

        _settings = new ObserverCommandSettings
        {
            EventStore = "the-event-store",
            Namespace = "the-namespace",
            ObserverId = "the-observer",
            EventSequenceId = "event-log"
        };

        _command = new RemoveObserverCommandForSpecs();
        Removal(ObserverRemovalOutcome.Removed);
    }

    protected void Removal(ObserverRemovalOutcome outcome, string blockingNamespace = "") =>
        _observers
            .RemoveObserver(Arg.Any<RemoveObserverContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new RemoveObserverResponse { Outcome = outcome, BlockingNamespace = blockingNamespace });

    protected Task<int> Execute() => _command.Execute(_services, _settings);

    /// <summary>
    /// Exposes the command's execution and its confirmation prompt, both of which are protected on the base command.
    /// </summary>
    public class RemoveObserverCommandForSpecs : RemoveObserverCommand
    {
        public Task<int> Execute(IServices services, ObserverCommandSettings settings) =>
            ExecuteCommandAsync(services, settings, "json");

        public string Confirmation(ObserverCommandSettings settings) => GetConfirmationPrompt(settings);
    }
}
