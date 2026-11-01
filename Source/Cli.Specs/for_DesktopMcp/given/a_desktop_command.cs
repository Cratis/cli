// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console.Cli;

namespace Cratis.Cli.for_DesktopMcp.given;

public class a_desktop_command : Specification
{
    private protected DesktopMcpRun _run;
    protected StringWriter _output;
    protected StringWriter _error;
    protected CommandContext _context;
    protected int _exitCode;

    void Establish()
    {
        _run = Substitute.For<DesktopMcpRun>();
        _run(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<TextWriter>(), Arg.Any<TextWriter>()).Returns(Task.FromResult(ExitCodes.ValidationError));
        _output = new();
        _error = new();
        _context = new CommandContext([], Substitute.For<IRemainingArguments>(), "desktop", null);
    }

    void Destroy()
    {
        _output.Dispose();
        _error.Dispose();
    }
}
