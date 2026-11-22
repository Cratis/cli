// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.Commands.Chronicle.Observers;
using Cratis.Cli.given;

namespace Cratis.Cli.for_ListObserversCommand.given;

[Collection(CliSpecsCollection.Name)]
public class a_list_observers_command : Specification
{
    protected IServices _services;
    protected IObservers _observers;
    protected ListObserversSettings _settings;
    protected ListObserversCommandForSpecs _command;
    protected List<ObserverInformation> _data;
    protected StringWriter _output;
    TextWriter _previousOutput;

    void Establish()
    {
        _services = Substitute.For<IServices>();
        _observers = Substitute.For<IObservers>();
        _services.Observers.Returns(_observers);
        _data = [];
        _observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(_ => _data);
        _settings = new ListObserversSettings { EventStore = "store", Namespace = "tenant" };
        _command = new ListObserversCommandForSpecs();
        _previousOutput = Console.Out;
        _output = new StringWriter();
        Console.SetOut(_output);
    }

    void Destroy() => Console.SetOut(_previousOutput);

    protected Task<int> Execute(string format) => _command.Execute(_services, _settings, format);

    public class ListObserversCommandForSpecs : ListObserversCommand
    {
        public Task<int> Execute(IServices services, ListObserversSettings settings, string format) => ExecuteCommandAsync(services, settings, format);
    }
}
