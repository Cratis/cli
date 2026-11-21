// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.Commands.Chronicle.Observers;
using Cratis.Cli.given;

namespace Cratis.Cli.for_ReplayObserverCommand.given;

public class a_replay_observer_command : a_temp_config_directory
{
    protected const string SubscriptionObserverId = "orders-subscription->consumer-store";
    protected const string ResolvedSubscriptionObserverId = "orders-forwarder";
    protected const string OrdinaryObserverId = "ordinary-observer";

    protected ReplayObserverCommandForSpecs _command;
    protected IServices _services;
    protected IObservers _observers;
    protected ObserverCommandSettings _settings;
    protected int _result;

    string? _previousNonInteractive;

    void Establish()
    {
        _previousNonInteractive = Environment.GetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable);
        Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, "1");

        _command = new();
        _observers = Substitute.For<IObservers>();
        _services = Substitute.For<IServices>();
        _services.Observers.Returns(_observers);
        _observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(
        [
            new ObserverInformation { Id = ResolvedSubscriptionObserverId, EventSequenceId = "outbox" },
            new ObserverInformation { Id = OrdinaryObserverId, EventSequenceId = "event-log" }
        ]);
        _settings = new ObserverCommandSettings
        {
            Output = OutputFormats.JsonCompact,
            Server = "chronicle://127.0.0.1:1",
            EventStore = "producer-store",
            Namespace = "producer-namespace"
        };
    }

    /// <inheritdoc/>
    protected override void CleanUp()
    {
        try
        {
            Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, _previousNonInteractive);
        }
        finally
        {
            base.CleanUp();
        }
    }
}
