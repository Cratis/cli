// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliEntryPoint.given;

public class a_protocol_invocation : Specification
{
    protected string _project;
    private protected IScreenplayMcpRunner _runner;
    protected StringReader _input;
    protected StringWriter _output;
    protected StringWriter _error;
    protected bool _interactiveStarted;
    protected int _exitCode;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), $"cratis mcp {Guid.NewGuid():N}");
        Directory.CreateDirectory(_project);
        _runner = Substitute.For<IScreenplayMcpRunner>();
        _input = new("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\"}\n");
        _output = new();
        _error = new();
    }

    protected Task<int> Invoke(params string[] args) => CliEntryPoint.Run(args, Interactive, _runner, _input, _output, _error, _project, _ => null);

    Task<int> Interactive()
    {
        _interactiveStarted = true;
        return Task.FromResult(0);
    }

    void Destroy()
    {
        _input.Dispose();
        _output.Dispose();
        _error.Dispose();
        Directory.Delete(_project, recursive: true);
    }
}
