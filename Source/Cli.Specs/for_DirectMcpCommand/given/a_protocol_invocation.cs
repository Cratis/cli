// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand.given;

public class a_protocol_invocation : Specification
{
    private protected IDirectMcpRunner _runner;
    protected StringReader _input;
    protected StringWriter _output;
    protected StringWriter _error;
    protected int _exitCode;

    void Establish()
    {
        _runner = Substitute.For<IDirectMcpRunner>();
        _input = new("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\"}\n");
        _output = new();
        _error = new();
    }

    protected Task<int> Invoke(DirectMcpSettings settings, params string[] unknown)
    {
        var remaining = Substitute.For<IRemainingArguments>();
        remaining.Raw.Returns(unknown);
        var command = new DirectMcpCommand(_runner, _input, _output, _error);
        return command.ExecuteAsync(new CommandContext([], remaining, "mcp", null), settings, CancellationToken.None);
    }

    void Destroy()
    {
        _input.Dispose();
        _output.Dispose();
        _error.Dispose();
    }
}
