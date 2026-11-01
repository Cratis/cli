// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Spectre.Console;

namespace Cratis.Cli.for_RenderCommand.given;

public class a_check_command : a_render_command
{
    protected string _destination = null!;

    void Establish()
    {
        _destination = Path.Combine(_folder, "out");
        _settings.Check = true;
        _settings.Destination = _destination;
        _command = new RenderCommand(_planning, new ArtifactPublisher());
    }

    protected static string[] Snapshot(string destination) => !Directory.Exists(destination)
        ? ["absent"]
        : ["directory", .. Directory.EnumerateDirectories(destination, "*", SearchOption.AllDirectories)
            .Select(path => $"directory:{Path.GetRelativePath(destination, path)}").Order(StringComparer.Ordinal),
            .. Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories)
                .Select(path => $"file:{Path.GetRelativePath(destination, path)}:{Convert.ToHexString(File.ReadAllBytes(path))}").Order(StringComparer.Ordinal)];

    protected async Task<(int ExitCode, string Stdout, string Stderr)> Capture()
    {
        var previousOutput = Console.Out;
        var previousError = Console.Error;
        var previousConsole = AnsiConsole.Console;
        await using var output = new StringWriter();
        await using var error = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(output), Ansi = AnsiSupport.No });
            AnsiConsole.Console.Profile.Width = 500;
            var exitCode = await Execute();
            return (exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(previousOutput);
            Console.SetError(previousError);
            AnsiConsole.Console = previousConsole;
        }
    }

    protected async Task RenderFirst()
    {
        _settings.Check = false;
        var rendered = await Capture();
        rendered.ExitCode.ShouldEqual(ExitCodes.Success);
        _settings.Check = true;
    }
}
