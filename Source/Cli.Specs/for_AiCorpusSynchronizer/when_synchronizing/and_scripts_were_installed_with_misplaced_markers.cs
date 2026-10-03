// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_scripts_were_installed_with_misplaced_markers : given.a_corpus_with_executable_scripts
{
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_project, ".cratis", "ai", "hooks"));
        var files = new List<AiManagedFile>();
        foreach (var name in _expected.Keys.Where(name => name.EndsWith(".mjs", StringComparison.Ordinal)))
        {
            var source = $"hooks/{name}";
            var content = File.ReadAllText(Path.Combine(_corpus, ".cratis", "ai", "hooks", name));
            var legacy = $"// cratis-ai-managed: {source}\n{content}";
            File.WriteAllText(Installed(name), legacy);
            files.Add(new(source, source, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(legacy)))));
        }
        File.WriteAllText(Path.Combine(_project, ".cratis", "ai.manifest.json"), JsonSerializer.Serialize(new AiInstallationManifest("previous", files)));
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);

    [Fact] void should_refresh_without_force() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_replace_old_markers_instead_of_duplicating_them() => _expected.ToDictionary(pair => pair.Key, pair => File.ReadAllText(Installed(pair.Key))).ShouldEqual(_expected);
    [Fact] void should_refresh_manifest_hashes() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
}
