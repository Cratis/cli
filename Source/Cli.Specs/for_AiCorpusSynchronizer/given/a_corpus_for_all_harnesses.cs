// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiCorpusSynchronizer.given;

public class a_corpus_for_all_harnesses : a_pi_corpus
{
    protected static AiConfiguration AllHarnesses { get; } = Configuration with { Harnesses = ["claude", "codex", "copilot", "pi", "cursor", "opencode"] };

    void Establish()
    {
        var path = Path.Combine(_corpus, ".cratis", "ai", "manifest.json");
        var manifest = JsonNode.Parse(File.ReadAllText(path));
        manifest["harnesses"] = JsonSerializer.SerializeToNode(AllHarnesses.Harnesses);
        File.WriteAllText(path, manifest.ToJsonString());
    }
}
