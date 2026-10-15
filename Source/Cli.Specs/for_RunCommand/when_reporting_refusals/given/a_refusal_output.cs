// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Spectre.Console;

namespace Cratis.Cli.for_RunCommand.when_reporting_refusals.given;

/// <summary>
/// Captures human and machine refusal output without starting a container.
/// </summary>
public class a_refusal_output : Specification
{
    protected StringWriter _output;
    protected StringWriter _error;
    protected StageRuntimeStatus _status = new("unsupported", "semantic", [
        new("Plan", "model", "one.play(1,1): Persona 'One' is authoring metadata."),
        new("Plan", "model", "two.play(2,1): Persona 'Two' is authoring metadata."),
        new("Plan", "model", "three.play(3,1): Persona 'Three' is authoring metadata."),
        new("Plan", "model", "four.play(4,1): Persona 'Four' is authoring metadata."),
        new("Plan", "model", "five.play(5,1): Persona 'Five' is authoring metadata."),
        new("Plan", "model", "six.play(6,1): Specification 'given' is missing required property 'id'."),
        new("Plan", "model", "seven.play(7,1): STAGE-ESM-005: Code validation is unsupported.")
    ]);

    TextWriter _previousOutput;
    TextWriter _previousError;
    IAnsiConsole _previousConsole;

    /// <summary>
    /// Counts the error panels in rendered output; Spectre trims the panel header and may fall back to ASCII borders.
    /// </summary>
    /// <param name="output">The rendered output.</param>
    /// <returns>The number of error panels.</returns>
    protected static int ErrorPanels(string output) => Regex.Matches(output, "[─-]Error[─-]", RegexOptions.None, TimeSpan.FromSeconds(1)).Count;

    void Establish()
    {
        _previousOutput = Console.Out;
        _previousError = Console.Error;
        _previousConsole = AnsiConsole.Console;
        _output = new StringWriter();
        _error = new StringWriter();
        Console.SetOut(_output);
        Console.SetError(_error);
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(_output), Ansi = AnsiSupport.No });
        AnsiConsole.Console.Profile.Width = 1000;
    }

    void Destroy()
    {
        AnsiConsole.Console = _previousConsole;
        Console.SetOut(_previousOutput);
        Console.SetError(_previousError);
        _output.Dispose();
        _error.Dispose();
    }
}
