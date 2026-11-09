// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.given;

/// <summary>
/// A Git repository to build into, with the shell kit and a generator that writes the slice's file and an outcome.
/// </summary>
public class a_project_repository : a_kit_folder
{
    protected string _project;
    protected string _workspaces;
    protected List<string> _log;
    protected string _fileContent = "built PayInvoice";
    protected string _agentOutcome = """{ "kind": "built", "files": ["src/PayInvoice.txt"], "specifications": [], "assumptions": [] }""";

    void Establish()
    {
        var root = Path.GetDirectoryName(_kit)!;
        _project = Path.Combine(root, "project");
        _workspaces = Path.Combine(root, "workspaces");
        _log = [];
        Directory.CreateDirectory(_project);
        File.WriteAllText(Path.Combine(_project, "README.md"), "# demo\n");
        Git("init", "-q", "-b", "main");
        Git("add", "README.md");
        Git("-c", "user.email=specs@cratis.io", "-c", "user.name=specs", "commit", "-qm", "init");
        Git("config", "user.email", "specs@cratis.io");
        Git("config", "user.name", "specs");
    }

    protected Task<JsonObject> Build(BuildGitInstructions git, BuildOrder? order = null)
    {
        var options = new BuildHostOptions(
            _project,
            BuildKit.Load(_kit),
            [],
            false,
            null,
            _ => [],
            session => new ExecGenerator(session, Generator()),
            new BuildRedaction([]),
            _log.Add)
        {
            WorkspaceRoot = _workspaces
        };

        return new BuildHost(options).Build(order ?? an_order.Parse(), git, CancellationToken.None);
    }

    string Generator()
    {
        var outcome = Path.Combine(Path.GetDirectoryName(_kit)!, "agent-outcome.json");
        File.WriteAllText(outcome, _agentOutcome);
        return $"mkdir -p src && echo '{_fileContent}' > src/PayInvoice.txt && cp '{outcome}' \"$STUDIO_BUILD_OUTCOME\"";
    }

    protected string Git(params string[] arguments)
    {
        var start = new System.Diagnostics.ProcessStartInfo("git") { WorkingDirectory = _project, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output.Trim();
    }
}
