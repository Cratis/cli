// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console;

namespace Cratis.Cli.for_UnexpectedErrorHandler.given;

[Collection(CliSpecsCollection.Name)]
public class an_error_handler : Specification
{
    protected StringWriter _output = null!;
    protected IAnsiConsole _console = null!;
    protected int _exitCode;
    protected Exception _failure = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        _output = new StringWriter();
        _previousError = Console.Error;
        Console.SetError(_output);
        _console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(_output), Ansi = AnsiSupport.No });
        _console.Profile.Width = 300;
        _failure = Catch.Exception(ThrowRendererFailure)!;
    }

    protected static void ThrowRendererFailure() => throw new ArgumentException("Unexpected renderer failure.");

    void Destroy()
    {
        Console.SetError(_previousError);
        _output.Dispose();
    }
}
