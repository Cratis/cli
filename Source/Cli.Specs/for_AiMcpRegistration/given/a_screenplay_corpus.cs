// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiMcpRegistration.given;

public class a_screenplay_corpus : Specification
{
    protected string _project;
    protected string _corpus;
    protected AiConfiguration _configuration;
    protected SyncResult _result;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), $"cratis-mcp-project-{Guid.NewGuid():N}");
        _corpus = Path.Combine(Path.GetTempPath(), $"cratis-mcp-corpus-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_project);
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis/ai/rules"));
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis/ai/harnesses/pi/extensions"));
        File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/harnesses/pi/extensions/cratis-mcp.ts"), "// Native bridge fixture\n");
        File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/manifest.json"), """
            {"harnesses":["claude","copilot","cursor","opencode","codex","pi"],"profiles":["cratis/stage","cratis/screenplay","cratis/other"],"languages":["csharp"]}
            """);
        File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/profile-catalog.json"), """
            {"publicProfiles":[{"id":"cratis/stage","composes":["cratis/screenplay"]},{"id":"cratis/screenplay","languages":["language-agnostic"]},{"id":"cratis/other"}],"engineeringProfiles":[]}
            """);
        File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/rules/general.md"), "# Rules\n");
        File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/mcp-servers.json"), """
            {"schemaVersion":"1.0","servers":[{"id":"screenplay","profiles":["cratis/screenplay"],"transport":"stdio","command":"cratis","args":["screenplay","mcp"],"description":"Screenplay model","defaultRoot":".cratis/screenplay"}]}
            """);
        _configuration = new(["claude", "copilot", "cursor", "opencode", "codex", "pi"], ["cratis/stage"], ["csharp"]);
    }

    protected string ProjectFile(string relative) => Path.Combine(_project, relative);
    protected JsonObject Read(string relative) => JsonNode.Parse(File.ReadAllText(ProjectFile(relative)))!.AsObject();
    protected void Write(string relative, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ProjectFile(relative))!);
        File.WriteAllText(ProjectFile(relative), content);
    }
    protected SyncResult Install(bool force = false, bool dryRun = false) => AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration, force, dryRun);

    void Destroy()
    {
        Directory.Delete(_project, recursive: true);
        Directory.Delete(_corpus, recursive: true);
    }
}
