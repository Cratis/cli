// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Mcp;
using Spectre.Console;

namespace Cratis.Cli.for_ValidateScreenplayCommand.given;

public class a_scoped_console : a_validate_screenplay_command
{
    protected int _errors = 2;
    protected int _warnings;
    protected string _output;
    protected int _exitCode;

    protected async Task CaptureScopedConsole()
    {
        _settings.Path = _document;
        _settings.Scope = "M";
        _settings.Output = OutputFormats.Table;
        var validated = new ValidatedScreenplay(1, [])
        {
            Scoped = new("M", 1, 0, [], [], new(2, ["", "Other.F"]), 3, "Source references only", _errors, _warnings)
        };
        _validation.TryValidateScoped(_document, "M", Arg.Any<CompletenessChecks>(), out Arg.Any<ValidatedScreenplay?>(), out Arg.Any<ScopeSelectionError?>())
            .Returns(call =>
            {
                call[3] = validated;
                call[4] = null;
                return true;
            });
        var previous = AnsiConsole.Console;
        await using var writer = new StringWriter();
        try
        {
            AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Out = new AnsiConsoleOutput(writer),
                Ansi = AnsiSupport.Yes,
                ColorSystem = ColorSystemSupport.TrueColor
            });
            AnsiConsole.Console.Profile.Width = 240;
            _exitCode = await Execute();
            _output = writer.ToString();
        }
        finally
        {
            AnsiConsole.Console = previous;
        }
    }
}
