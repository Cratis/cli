// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_AiCorpusSynchronizer;

public class when_update_or_uninstall_reads_unowned_manifest_entries : given.an_ownership_plan
{
    const string LocalContent = "Local notes";
    const string LocalLink = ".pi/personal.md";
    const string LocalTarget = "../.cratis/ai/rules/general.md";
    const string RetiredAgent = "agents/screenplay-modeler.md";
    string[] _destinations = null!;

    void Establish()
    {
        _destinations = ["local/notes.md", "../x", Path.Combine(_project, "absolute.txt"), "rules/project/notes.md", "skills/notes.md", "skills/example/verification.json"];
        var manifest = JsonSerializer.Deserialize<AiInstallationManifest>(_originalManifest)!;
        var entries = _destinations.Select(destination =>
        {
            var path = ManagedPath(destination);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, LocalContent);
            return new AiManagedFile(destination, destination, AiCorpusSynchronizer.Hash(LocalContent));
        });
        File.CreateSymbolicLink(Path.Combine(_project, LocalLink), LocalTarget);
        File.WriteAllText(ManifestPath, JsonSerializer.Serialize(manifest with
        {
            Files = [.. manifest.Files, .. entries],
            Integrations = [.. manifest.Integrations!, new(LocalLink, LocalTarget, false)]
        }));
        File.Delete(Path.Combine(_corpus, AiCorpusSynchronizer.ManagedRoot, RetiredAgent));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    void should_keep_unowned_paths_and_report_them_while_removing_managed_paths(bool uninstall, bool force)
    {
        var result = uninstall
            ? AiCorpusSynchronizer.Uninstall(_project, force)
            : AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration, force);

        result.Conflicts.ShouldBeEmpty();
        foreach (var destination in _destinations)
        {
            File.ReadAllText(ManagedPath(destination)).ShouldEqual(LocalContent);
        }
        new FileInfo(Path.Combine(_project, LocalLink)).LinkTarget.ShouldEqual(LocalTarget);
        result.Warnings.ShouldNotBeNull();
        foreach (var path in _destinations.Append(LocalLink))
        {
            result.Warnings.ShouldContain(warning => warning.Contains(JsonSerializer.Serialize(path), StringComparison.Ordinal));
        }
        File.Exists(ManagedPath(RetiredAgent)).ShouldBeFalse();
        AiCorpusSynchronizer.PathExists(Path.Combine(_project, ".pi/agents")).ShouldBeFalse();
        File.Exists(Installed("general.md")).ShouldEqual(!uninstall);
    }

    string ManagedPath(string destination) => Path.GetFullPath(Path.Combine(_project, AiCorpusSynchronizer.ManagedRoot, destination));
}
