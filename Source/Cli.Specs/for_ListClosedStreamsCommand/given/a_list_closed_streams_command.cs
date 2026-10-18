// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Cli.Commands.Chronicle.Streams;
using Cratis.Cli.given;

namespace Cratis.Cli.for_ListClosedStreamsCommand.given;

[Collection(CliSpecsCollection.Name)]
public class a_list_closed_streams_command : Specification
{
    protected IServices _services;
    protected IEventSequences _sequences;
    protected StreamScopeSettings _settings;
    protected ListClosedStreamsCommandForSpecs _command;
    protected QueryResult<IEnumerable<ClosedStreamResponse>> _response;
    protected StringWriter _output;
    protected StringWriter _error;
    TextWriter _previousOutput;
    TextWriter _previousError;

    void Establish()
    {
        _services = Substitute.For<IServices>();
        _sequences = Substitute.For<IEventSequences>();
        _services.Sequences.Returns(_sequences);
        _response = new QueryResult<IEnumerable<ClosedStreamResponse>>
        {
            Data =
            [
                new()
                {
                    EventSourceId = "order-42",
                    EventSourceType = "Order",
                    EventStreamType = "Accounting",
                    EventStreamId = "2026",
                    Origin = ClosedStreamOrigin.ClosingEvent,
                    ClosedBy = "CloseBooks",
                    SequenceNumber = 123,
                    ClosedAt = DateTimeOffset.Parse("2026-10-10T12:00:00+00:00")
                }
            ]
        };
        _sequences.ClosedStreams(Arg.Any<ClosedStreamsRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(_ => _response);
        _settings = new StreamScopeSettings { EventStore = "store", Namespace = "tenant", EventSequenceId = "outbox" };
        _command = new ListClosedStreamsCommandForSpecs();
        _previousOutput = Console.Out;
        _previousError = Console.Error;
        _output = new StringWriter();
        _error = new StringWriter();
        Console.SetOut(_output);
        Console.SetError(_error);
    }

    protected Task<int> Execute(string format = "json") => _command.Execute(_services, _settings, format);

    void Destroy()
    {
        Console.SetOut(_previousOutput);
        Console.SetError(_previousError);
    }

    public class ListClosedStreamsCommandForSpecs : ListClosedStreamsCommand
    {
        public Task<int> Execute(IServices services, StreamScopeSettings settings, string format) => ExecuteCommandAsync(services, settings, format);
    }
}
