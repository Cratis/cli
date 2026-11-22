// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.Commands.Chronicle.Observers;
using Cratis.Cli.given;

namespace Cratis.Cli.for_ShowObserverCommand.given;

[Collection(CliSpecsCollection.Name)]
public class a_show_observer_command : Specification
{
    protected IServices _services;
    protected IObservers _observers;
    protected ObserverCommandSettings _settings;
    protected ShowObserverCommandForSpecs _command;
    protected ObserverInformation _info;
    protected StringWriter _output;
    TextWriter _previousOutput;

    void Establish()
    {
        _services = Substitute.For<IServices>();
        _observers = Substitute.For<IObservers>();
        _services.Observers.Returns(_observers);
        _info = new ObserverInformation { Id = "obs-1", EventSequenceId = "event-log" };
        _observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(_ => [_info]);
        _observers.GetObserverInformation(Arg.Any<GetObserverInformationRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(_ => _info);
        _settings = new ObserverCommandSettings { EventStore = "store", Namespace = "tenant", ObserverId = "obs-1" };
        _command = new ShowObserverCommandForSpecs();
        _previousOutput = Console.Out;
        _output = new StringWriter();
        Console.SetOut(_output);
    }

    void Destroy() => Console.SetOut(_previousOutput);

    protected Task<int> Execute(string format) => _command.Execute(_services, _settings, format);

    public class ShowObserverCommandForSpecs : ShowObserverCommand
    {
        public Task<int> Execute(IServices services, ObserverCommandSettings settings, string format) => ExecuteCommandAsync(services, settings, format);
    }
}
