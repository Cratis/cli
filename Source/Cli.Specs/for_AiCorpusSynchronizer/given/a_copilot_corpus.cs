// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.given;

public class a_copilot_corpus : a_corpus_with_profile_scoped_rules
{
    protected SyncResult _result = null!;
    protected static AiConfiguration Configuration { get; } = new(["copilot"], ["cratis/example"], []);
    protected string InstructionsTarget => new DirectoryInfo(Path.Combine(_project, ".github", "instructions")).LinkTarget!;

    void Establish() => File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"copilot\"],\"profiles\":[\"cratis/example\"],\"languages\":[]}");

    protected void AddAdapter()
    {
        var instructions = Path.Combine(_corpus, ".cratis", "ai", "harnesses", "copilot", "instructions");
        Directory.CreateDirectory(instructions);
        File.WriteAllText(Path.Combine(instructions, "general.instructions.md"), "---\napplyTo: \"**/*\"\n---\n# General rule");
    }
}
