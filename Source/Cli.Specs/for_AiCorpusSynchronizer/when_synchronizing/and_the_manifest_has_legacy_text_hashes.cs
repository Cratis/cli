// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_the_manifest_has_legacy_text_hashes : given.a_corpus_with_profile_scoped_rules
{
    static readonly AiConfiguration _configuration = new([], ["cratis/example"], []);
    SyncResult _result = null!;

    void Establish()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);
        var manifestPath = Path.Combine(_project, ".cratis", "ai.manifest.json");
        var manifest = JsonSerializer.Deserialize<AiInstallationManifest>(File.ReadAllText(manifestPath))!;
        var files = manifest.Files.Select(file => file with
        {
            Hash = AiCorpusSynchronizer.Hash(File.ReadAllText(Path.Combine(_project, AiCorpusSynchronizer.ManagedRoot, file.Destination)))
        }).ToArray();
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest with { FileHashVersion = null, Files = files }));

        var path = Installed("general.md");
        File.WriteAllText(path, File.ReadAllText(path), Encoding.Unicode);
        File.AppendAllText(Path.Combine(_corpus, AiCorpusSynchronizer.ManagedRoot, "rules", "general.md"), "\nUpdated rule");
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);

    [Fact] void should_update_without_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_install_the_updated_rule() => File.ReadAllText(Installed("general.md")).ShouldContain("Updated rule");
    [Fact] void should_record_matching_hashes() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
}
