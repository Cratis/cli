// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.given;
using Grpc.Core;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Cli.for_ReplayExistingConsumerCommand;

[Collection(CliSpecsCollection.Name)]
[PerFactSpecificationLifecycle]
public class when_dispatching_explicit_replay : Specification
{
    IServices _services;
    IObservers _observers;
    DispatchCommand _command;
    ReplayExistingConsumerSettings _settings;

    void Establish()
    {
        _observers = Substitute.For<IObservers>();
        _services = Substitute.For<IServices>();
        _services.Observers.Returns(new an_observers_client_shaped_like_the_generated_proxy(_observers));
        _command = new DispatchCommand();
        _settings = new ReplayExistingConsumerSettings
        {
            EventStore = "producer",
            Namespace = "producer-namespace",
            ObserverId = "existing-observer",
            EventSequenceId = "outbox",
            SubscriptionId = "not-a-routing-value",
            ConsumerIdentity = "also-not-a-routing-value"
        };
    }

    [Fact]
    public async Task should_route_only_the_four_explicit_producer_identifiers()
    {
        await _command.Dispatch(_services, _settings);

        await _observers.Received(1).Replay(Arg.Is<Replay>(_ => _.EventStore == "producer" && _.Namespace == "producer-namespace" && _.EventSequenceId == "outbox" && _.ObserverId == "existing-observer"), Arg.Any<ProtoBuf.Grpc.CallContext>());
        _observers.ReceivedCalls().Count().ShouldEqual(1);
        _services.ReceivedCalls().Count().ShouldEqual(1);
    }

    [Fact]
    public async Task should_not_change_routing_after_the_operator_changes_labels()
    {
        await _command.Dispatch(_services, _settings);
        _settings.SubscriptionId = "another-subscription";
        _settings.ConsumerIdentity = "another-consumer";

        await _command.Dispatch(_services, _settings);

        await _observers.Received(2).Replay(Arg.Is<Replay>(_ => _.EventStore == "producer" && _.Namespace == "producer-namespace" && _.EventSequenceId == "outbox" && _.ObserverId == "existing-observer"), Arg.Any<ProtoBuf.Grpc.CallContext>());
        _observers.ReceivedCalls().Count().ShouldEqual(2);
    }

    [Fact]
    public async Task should_surface_rpc_failure_as_an_attempted_dispatch_not_zero_rpc()
    {
        _observers.Replay(Arg.Any<Replay>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).ThrowsAsync(new RpcException(new Status(StatusCode.Internal, "Replay failed")));

        var error = await Catch.Exception(() => _command.Dispatch(_services, _settings));

        error.ShouldBeOfExactType<RpcException>();
        await _observers.Received(1).Replay(Arg.Any<Replay>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    }

    [Fact]
    public async Task should_preserve_ordinary_replay_routing()
    {
        var ordinary = new OrdinaryReplay();
        await ordinary.Dispatch(_services, new ObserverCommandSettings { EventStore = "ordinary-store", Namespace = "ordinary-namespace", EventSequenceId = "event-log", ObserverId = "ordinary-observer" });

        await _observers.Received(1).Replay(Arg.Is<Replay>(_ => _.EventStore == "ordinary-store" && _.Namespace == "ordinary-namespace" && _.EventSequenceId == "event-log" && _.ObserverId == "ordinary-observer"), Arg.Any<ProtoBuf.Grpc.CallContext>());
    }

    sealed class DispatchCommand : ReplayExistingConsumerCommand
    {
        public Task<int> Dispatch(IServices services, ReplayExistingConsumerSettings settings) => ExecuteCommandAsync(services, settings, OutputFormats.Quiet);
    }

    sealed class OrdinaryReplay : ReplayObserverCommand
    {
        public Task<int> Dispatch(IServices services, ObserverCommandSettings settings) => ExecuteCommandAsync(services, settings, OutputFormats.Quiet);
    }
}
