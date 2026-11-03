// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_AiCorpusSynchronizer.given;

public class an_old_pi_installation : a_pi_corpus
{
    void Establish()
    {
        AddAdapter();
        AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);
        Directory.Delete(Path.Combine(_project, ".pi", "agents"));
        Directory.CreateSymbolicLink(Path.Combine(_project, ".pi", "agents"), "../.cratis/ai/agents");
        var manifest = JsonSerializer.Deserialize<AiInstallationManifest>(InstalledManifest);
        var integrations = manifest.Integrations.Select(integration => integration.Path == ".pi/agents"
            ? integration with { Target = "../.cratis/ai/agents" }
            : integration).ToArray();
        File.WriteAllText(Path.Combine(_project, ".cratis", "ai.manifest.json"), JsonSerializer.Serialize(manifest with { Integrations = integrations }));
    }
}
