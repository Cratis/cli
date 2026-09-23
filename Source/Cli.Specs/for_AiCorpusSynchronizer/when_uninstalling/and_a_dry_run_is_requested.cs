// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_uninstalling;

/// <summary>Specifies that a dry run of uninstall reports the removals and deletes nothing.</summary>
/// <remarks>
/// Uninstall deletes, so it is the command a dry run is worth most on, and the one where a missed guard
/// costs the most. The installed tree is fingerprinted after the install and required to be identical
/// after the dry run.
/// </remarks>
public class and_a_dry_run_is_requested : Specification
{
    string _project = null!;
    string _corpus = null!;
    SyncResult _result = null!;
    IReadOnlyDictionary<string, string> _before = null!;
    IReadOnlyDictionary<string, string> _after = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "rules"));
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "skills", "example"));
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/example\"],\"languages\":[\"csharp\"]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/example\",\"availableTargets\":[\"example\"]}],\"engineeringProfiles\":[]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "rules", "general.md"), "# Rule");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "skills", "example", "SKILL.md"), "# Skill");
        AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi"], ["cratis/example"], ["csharp"]));
        _before = Fingerprint(_project);
    }

    void Because()
    {
        _result = AiCorpusSynchronizer.Uninstall(_project, dryRun: true);
        _after = Fingerprint(_project);
    }

    /// <summary>Guards the comparison below, which holds trivially on a run that planned nothing at all.</summary>
    [Fact] void should_report_the_removals_it_would_make() => _result.Actions.ShouldNotBeEmpty();
    [Fact] void should_leave_the_installation_byte_for_byte_unchanged() => _after.ShouldEqual(_before);
    [Fact] void should_keep_the_manifest() => File.Exists(Path.Combine(_project, ".cratis", "ai.manifest.json")).ShouldBeTrue();

    static IReadOnlyDictionary<string, string> Fingerprint(string root)
    {
        var fingerprints = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!Directory.Exists(root)) return fingerprints;
        foreach (var path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            fingerprints[relative] = Directory.Exists(path) || new DirectoryInfo(path).LinkTarget is not null
                ? "<directory-or-link>"
                : Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        }

        return fingerprints;
    }

    void Destroy()
    {
        Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
