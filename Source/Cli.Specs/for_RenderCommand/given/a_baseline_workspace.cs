// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Screenplay.Workspaces;
using Spectre.Console;

namespace Cratis.Cli.for_RenderCommand.given;

public class a_baseline_workspace : a_workspace_render_command
{
    protected string _baselinePath = null!;
    protected string _implicitPath = null!;
    protected (int ExitCode, string Stdout, string Stderr) _output;

    void Establish()
    {
        var workspace = CreateWorkspace();
        WriteWorkspace(workspace);
        var admitted = ReleaseBaseline.Admit(workspace, ReleaseBaselineStore.ReadBytes(null, workspace.IdentityCatalog.Application), "v1.0", DateTimeOffset.UnixEpoch);
        var document = admitted.Document!;
        var entry = document.Entries[0];
        var schema = entry.Schema with { Properties = entry.Schema.Properties.SetItem(0, entry.Schema.Properties[0] with { Optional = !entry.Schema.Properties[0].Optional }) };
        document = document with { Entries = document.Entries.SetItem(0, entry with { Schema = schema, SchemaHash = ReleaseSchemaCanonical.Hash(schema) }) };
        _baselinePath = Path.Combine(_folder, "selected-baseline.json");
        _implicitPath = Path.Combine(Directory.CreateDirectory(Path.Combine(_folder, ".screenplay")).FullName, ReleaseBaselineStore.FileName);
        File.WriteAllBytes(_baselinePath, ReleaseBaselineStore.Serialize(document));
        _settings.Baseline = _baselinePath;
        _publication.Check(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>()).Returns(call => new ArtifactPublisher().Check(call.Arg<ArtifactPublicationRequest>(), call.Arg<CancellationToken>()));
    }

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
}
