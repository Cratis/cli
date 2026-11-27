// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_AiCorpusSynchronizer.given;

public class an_installation_with_agents_and_prompts : a_corpus_for_all_harnesses
{
    protected const string GuardHook = "cratis-guard-writes.sh";
    protected const string RetiredHook = "cratis-pattern-scan.sh";

    protected static readonly string[] RetiredLinks =
    [
        ".claude/agents", ".claude/prompts", ".claude/commands/example.md",
        ".github/prompts", ".github/agents/screenplay-modeler.agent.md", ".pi/agents", ".pi/prompts/example.md",
        ".cursor/agents", ".cursor/commands/example.md", ".opencode/agents", ".opencode/commands/example.md"
    ];

    protected string CorpusRoot => Path.Combine(_corpus, ".cratis", "ai");
    protected string ClaudeSettings => Path.Combine(_project, ".claude", "settings.json");
    protected IReadOnlyList<AiManagedIntegration> RecordedLinks => JsonSerializer.Deserialize<AiInstallationManifest>(InstalledManifest)!.Integrations!;

    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(CorpusRoot, "prompts"));
        Directory.CreateDirectory(Path.Combine(CorpusRoot, "hooks"));
        File.WriteAllText(Path.Combine(CorpusRoot, "prompts", "example.prompt.md"), "# Prompt");
        WriteHooks(GuardHook, RetiredHook);
        AiCorpusSynchronizer.Synchronize(_project, _corpus, AllHarnesses);
    }

    protected void RetireAgentsPromptsAndTheScanHook()
    {
        Directory.Delete(Path.Combine(CorpusRoot, "agents"), true);
        Directory.Delete(Path.Combine(CorpusRoot, "prompts"), true);
        WriteHooks(GuardHook);
    }

    protected bool Occupied(string path) => AiCorpusSynchronizer.PathExists(Path.Combine(_project, path));

    void WriteHooks(params string[] scripts) => File.WriteAllText(
        Path.Combine(CorpusRoot, "hooks", "settings.template.json"),
        JsonSerializer.Serialize(new { hooks = scripts.Select(script => new { command = $"bash .cratis/ai/hooks/scripts/{script}" }) }));
}
