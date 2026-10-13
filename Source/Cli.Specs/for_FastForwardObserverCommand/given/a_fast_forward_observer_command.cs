// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.Commands.Chronicle.Observers;
using Cratis.Cli.given;
using FastForwardContract = Cratis.Chronicle.Contracts.Observation.FastForward;

namespace Cratis.Cli.for_FastForwardObserverCommand.given;

[Collection(CliSpecsCollection.Name)]
public class a_fast_forward_observer_command : Specification
{
    protected IServices _services;
    protected IObservers _observers;
    protected ObserverCommandSettings _settings;
    protected FastForwardObserverCommandForSpecs _command;
    protected StringWriter _output;
    protected StringWriter _error;
    TextWriter _previousOutput;
    TextWriter _previousError;

    void Establish()
    {
        _observers = Substitute.For<IObservers>();
        _services = Substitute.For<IServices>();

        // Explicitly implemented, like the shipped client - this double is also the guard that the contract changed.
        _services.Observers.Returns(new an_observers_client_shaped_like_the_generated_proxy(_observers));

        _settings = new ObserverCommandSettings
        {
            EventStore = "the-event-store",
            Namespace = "the-namespace",
            ObserverId = "the-observer",
            EventSequenceId = "event-log"
        };

        _command = new FastForwardObserverCommandForSpecs();
        _previousOutput = Console.Out;
        _previousError = Console.Error;
        _output = new StringWriter();
        _error = new StringWriter();
        Console.SetOut(_output);
        Console.SetError(_error);
        Responds(FastForwardOutcome.FastForwarded);
    }

    protected void Responds(FastForwardOutcome outcome) =>
        _observers
            .FastForward(Arg.Any<FastForwardContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new FastForwardResponse { Outcome = outcome, PreviousNextEventSequenceNumber = 5, NextEventSequenceNumber = 101 });

    protected Task<int> Execute() => _command.Execute(_services, _settings);

    void Destroy()
    {
        Console.SetOut(_previousOutput);
        Console.SetError(_previousError);
    }

    public class FastForwardObserverCommandForSpecs : FastForwardObserverCommand
    {
        public Task<int> Execute(IServices services, ObserverCommandSettings settings) =>
            ExecuteCommandAsync(services, settings, "json");
    }
}
