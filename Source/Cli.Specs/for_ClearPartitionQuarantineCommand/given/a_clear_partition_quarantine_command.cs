// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.Commands.Chronicle.Observers;
using Cratis.Cli.given;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.given;

[Collection(CliSpecsCollection.Name)]
public class a_clear_partition_quarantine_command : Specification
{
    protected IServices _services;
    protected IObservers _observers;
    protected ClearPartitionQuarantineSettings _settings;
    protected ClearPartitionQuarantineCommandForSpecs _command;
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

        _settings = new ClearPartitionQuarantineSettings
        {
            EventStore = "the-event-store",
            Namespace = "the-namespace",
            ObserverId = "the-observer",
            Partition = "the-partition",
            EventSequenceId = "event-log"
        };

        _command = new ClearPartitionQuarantineCommandForSpecs();
        _previousOutput = Console.Out;
        _previousError = Console.Error;
        _output = new StringWriter();
        _error = new StringWriter();
        Console.SetOut(_output);
        Console.SetError(_error);
        Responds(ClearPartitionQuarantineOutcome.Cleared, PartitionRecoveryOutcome.Started);
    }

    protected void Responds(ClearPartitionQuarantineOutcome outcome, PartitionRecoveryOutcome retryOutcome = PartitionRecoveryOutcome.Started) =>
        _observers
            .ClearPartitionQuarantine(Arg.Any<ClearPartitionQuarantineContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new ClearPartitionQuarantineResponse { Outcome = outcome, RetryOutcome = retryOutcome });

    protected Task<int> Execute() => _command.Execute(_services, _settings);

    void Destroy()
    {
        Console.SetOut(_previousOutput);
        Console.SetError(_previousError);
    }

    public class ClearPartitionQuarantineCommandForSpecs : ClearPartitionQuarantineCommand
    {
        public Task<int> Execute(IServices services, ClearPartitionQuarantineSettings settings) =>
            ExecuteCommandAsync(services, settings, "json");
    }
}
