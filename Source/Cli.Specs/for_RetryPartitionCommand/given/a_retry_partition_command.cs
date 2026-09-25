// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.Commands.Chronicle.Observers;
using Cratis.Cli.given;
using RetryPartitionContract = Cratis.Chronicle.Contracts.Observation.RetryPartition;

namespace Cratis.Cli.for_RetryPartitionCommand.given;

public class a_retry_partition_command : Specification
{
    protected IServices _services;
    protected IObservers _observers;
    protected PartitionCommandSettings _settings;
    protected RetryPartitionCommandForSpecs _command;

    void Establish()
    {
        _observers = Substitute.For<IObservers>();
        _services = Substitute.For<IServices>();

        // Explicitly implemented, like the shipped client - see the double's own remarks and issue #185.
        _services.Observers.Returns(new an_observers_client_shaped_like_the_generated_proxy(_observers));

        _settings = new PartitionCommandSettings
        {
            EventStore = "the-event-store",
            Namespace = "the-namespace",
            ObserverId = "the-observer",
            Partition = "the-partition",
            EventSequenceId = "event-log"
        };

        _command = new RetryPartitionCommandForSpecs();
        Recovery(PartitionRecoveryOutcome.Started);
    }

    protected void Recovery(PartitionRecoveryOutcome outcome) =>
        _observers
            .RetryPartition(Arg.Any<RetryPartitionContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new RetryPartitionResponse { Outcome = outcome });

    protected Task<int> Execute() => _command.Execute(_services, _settings);

    public class RetryPartitionCommandForSpecs : RetryPartitionCommand
    {
        public Task<int> Execute(IServices services, PartitionCommandSettings settings) =>
            ExecuteCommandAsync(services, settings, "json");
    }
}
