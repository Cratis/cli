// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;
using Spectre.Console;

namespace Cratis.Cli.for_NewCommand.given;

/// <summary>
/// Writes a small local template and captures what the new command prints for it, without acquiring a package.
/// </summary>
public class a_local_template : Specification
{
    protected string _templates;
    protected string _output;
    protected StringWriter _console;

    TextWriter _previousOutput;
    IAnsiConsole _previousConsole;

    /// <summary>
    /// Runs the new command against the local template with the given arguments.
    /// </summary>
    /// <param name="arguments">The arguments after the command name.</param>
    /// <returns>The exit code.</returns>
    protected Task<int> New(params string[] arguments) =>
        CliApp.Create().RunAsync(["new", "--template-path", _templates, .. arguments], TestContext.Current.CancellationToken);

    void Establish()
    {
        _templates = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-new", Guid.NewGuid().ToString("N"))).FullName;
        _output = Path.Combine(_templates, "out");
        var template = Directory.CreateDirectory(Path.Combine(_templates, "sample"));
        Directory.CreateDirectory(Path.Combine(template.FullName, ".template.config"));
        File.WriteAllText(Path.Combine(template.FullName, ".template.config", "template.json"), string.Join('\n', [
            "{",
            "  \"identity\": \"Spec.Sample\",",
            "  \"name\": \"Sample\",",
            "  \"shortName\": \"sample\",",
            "  \"sourceName\": \"Sample.App\",",
            "  \"symbols\": { \"Greeting\": { \"type\": \"parameter\", \"datatype\": \"text\", \"defaultValue\": \"Hello\", \"replaces\": \"GREETING\" } },",
            "  \"postActions\": [ { \"actionId\": \"00000000-0000-0000-0000-000000000001\", \"description\": \"Follow up\" } ]",
            "}"]));
        File.WriteAllText(Path.Combine(template.FullName, "Sample.App.txt"), "GREETING from Sample.App\n");

        NewCommandArguments.Partition([]);
        _previousOutput = Console.Out;
        _previousConsole = AnsiConsole.Console;
        _console = new StringWriter();
        Console.SetOut(_console);
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(_console), Ansi = AnsiSupport.No });
        AnsiConsole.Console.Profile.Width = 1000;
    }

    void Destroy()
    {
        AnsiConsole.Console = _previousConsole;
        Console.SetOut(_previousOutput);
        _console.Dispose();
        if (Directory.Exists(_templates))
        {
            Directory.Delete(_templates, recursive: true);
        }
    }
}
