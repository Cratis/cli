// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.given;

public class a_pi_corpus : a_corpus_with_profile_scoped_rules
{
    protected const string CanonicalAgent = "---\nname: Screenplay Modeler\ndescription: Models\ntools:\n  - Read\n  - Grep\n  - Glob\n  - Bash\n  - Edit\n  - Write\n---\n# Agent";
    protected const string PiAgent = "---\nname: Screenplay Modeler\ndescription: Models\ntools: read, grep, find, bash, edit, write\nreadonly: false\n---\n# Agent";
    protected SyncResult _result = null!;
    protected static AiConfiguration Configuration { get; } = new(["pi"], ["cratis/example"], []);
    protected string AgentsTarget => new DirectoryInfo(Path.Combine(_project, ".pi", "agents")).LinkTarget!;
    protected string InstalledManifest => File.ReadAllText(Path.Combine(_project, ".cratis", "ai.manifest.json"));

    void Establish()
    {
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/example\"],\"languages\":[]}");
        var agents = Path.Combine(_corpus, ".cratis", "ai", "agents");
        Directory.CreateDirectory(agents);
        File.WriteAllText(Path.Combine(agents, "screenplay-modeler.md"), CanonicalAgent);
    }

    protected void AddAdapter()
    {
        var agents = Path.Combine(_corpus, ".cratis", "ai", "harnesses", "pi", "agents");
        Directory.CreateDirectory(agents);
        File.WriteAllText(Path.Combine(agents, "screenplay-modeler.md"), PiAgent);
    }

    protected string ToolsOf(string agentPath) => File.ReadAllLines(agentPath).Single(line => line.StartsWith("tools:", StringComparison.Ordinal))["tools:".Length..].Trim();
}
