// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_AiCorpusSynchronizer.given;

public class an_old_claude_installation : a_corpus_with_profile_scoped_rules
{
    protected SyncResult _result = null!;
    protected static AiConfiguration Configuration { get; } = new(["claude"], ["cratis/example"], []);
    protected string NestedPath => Path.Combine(_project, ".claude", "CLAUDE.md");
    string ManifestPath => Path.Combine(_project, ".cratis", "ai.manifest.json");
    protected AiManagedIntegration NestedRecord => JsonSerializer.Deserialize<AiInstallationManifest>(File.ReadAllText(ManifestPath)).Integrations.Single(integration => integration.Path == ".claude/CLAUDE.md");

    void Establish()
    {
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"claude\"],\"profiles\":[\"cratis/example\"],\"languages\":[]}");
        AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);
        var manifest = JsonSerializer.Deserialize<AiInstallationManifest>(File.ReadAllText(ManifestPath));
        var integrations = manifest.Integrations.Select(integration => integration.Path == ".claude/CLAUDE.md"
            ? integration with { PreserveExisting = false }
            : integration).ToArray();
        File.WriteAllText(ManifestPath, JsonSerializer.Serialize(manifest with { Integrations = integrations }));
    }
}
