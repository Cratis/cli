// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Cli.Commands.Chronicle.Streams;
using Cratis.Cli.given;

namespace Cratis.Cli.for_ReopenClosedStreamCommand.given;

[Collection(CliSpecsCollection.Name)]
public class a_reopen_closed_stream_command : Specification
{
    protected IServices _services;
    protected IEventSequences _sequences;
    protected ReopenClosedStreamSettings _settings;
    protected ReopenClosedStreamCommandForSpecs _command;
    protected CommandResult<ReopenStreamScopeResponse> _response;
    protected StringWriter _output;
    protected StringWriter _error;
    TextWriter _previousOutput;
    TextWriter _previousError;

    void Establish()
    {
        _services = Substitute.For<IServices>();
        _sequences = Substitute.For<IEventSequences>();
        _services.Sequences.Returns(_sequences);
        _response = new CommandResult<ReopenStreamScopeResponse> { Response = new() { IsSuccess = true } };
        _sequences.ReopenStreamScope(Arg.Any<ReopenStreamScopeRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(_ => _response);
        _settings = new ReopenClosedStreamSettings
        {
            EventStore = "store",
            Namespace = "tenant",
            EventSequenceId = "outbox",
            EventSourceId = "order-42",
            EventSourceType = "Order",
            EventStreamType = "Accounting",
            EventStreamId = "2026",
            Reason = "Closed the wrong order"
        };
        _command = new ReopenClosedStreamCommandForSpecs();
        _previousOutput = Console.Out;
        _previousError = Console.Error;
        _output = new StringWriter();
        _error = new StringWriter();
        Console.SetOut(_output);
        Console.SetError(_error);
    }

    protected Task<int> Execute() => _command.Execute(_services, _settings);

    void Destroy()
    {
        Console.SetOut(_previousOutput);
        Console.SetError(_previousError);
    }

    public class ReopenClosedStreamCommandForSpecs : ReopenClosedStreamCommand
    {
        public Task<int> Execute(IServices services, ReopenClosedStreamSettings settings) => ExecuteCommandAsync(services, settings, "json");

        public string Prompt(ReopenClosedStreamSettings settings) => GetConfirmationPrompt(settings);
    }
}
