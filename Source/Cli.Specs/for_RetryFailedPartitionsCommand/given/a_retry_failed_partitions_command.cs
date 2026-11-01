// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.Commands.Chronicle.Observers;
using Cratis.Cli.given;

namespace Cratis.Cli.for_RetryFailedPartitionsCommand.given;

[Collection(CliSpecsCollection.Name)]
public class a_retry_failed_partitions_command : Specification
{
    protected IServices _services;
    protected IObservers _observers;
    protected ObserverCommandSettings _settings;
    protected RetryFailedPartitionsCommandForSpecs _command;
    protected StringWriter _output;
    TextWriter _previousOutput;

    void Establish()
    {
        _observers = Substitute.For<IObservers>();
        _services = Substitute.For<IServices>();
        _services.Observers.Returns(new an_observers_client_shaped_like_the_generated_proxy(_observers));
        _settings = new ObserverCommandSettings
        {
            EventStore = "the-event-store",
            Namespace = "the-namespace",
            ObserverId = "the-observer",
            EventSequenceId = "event-log"
        };
        _command = new RetryFailedPartitionsCommandForSpecs();
        _previousOutput = Console.Out;
        _output = new StringWriter();
        Console.SetOut(_output);
    }

    void Destroy() => Console.SetOut(_previousOutput);

    protected void FailedPartitions(int count) =>
        _observers
            .RetryFailedPartitions(Arg.Any<RetryFailedPartitions>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new RetryFailedPartitionsResponse { FailedPartitions = count });

    protected Task<int> Execute() => _command.Execute(_services, _settings);

    public class RetryFailedPartitionsCommandForSpecs : RetryFailedPartitionsCommand
    {
        public Task<int> Execute(IServices services, ObserverCommandSettings settings) =>
            ExecuteCommandAsync(services, settings, "json");
    }
}
