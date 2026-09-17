// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

/// <summary>Specifies that a dry run reports the changes and writes nothing.</summary>
/// <remarks>
/// A dry run that writes is worse than no dry run, because it is trusted. Comparing the actions it reports
/// against the actions a real run makes is not enough on its own - that passes while a stray write slips
/// through - so this fingerprints every path under the project before and after and requires them equal.
/// <para>
/// Every observation about the project is taken before the real run happens, or the real run's own writes
/// would answer the question instead.
/// </para>
/// </remarks>
public class and_a_dry_run_is_requested : Specification
{
    string _project = null!;
    string _corpus = null!;
    SyncResult _result = null!;
    SyncResult _performed = null!;
    IReadOnlyDictionary<string, string> _before = null!;
    IReadOnlyDictionary<string, string> _after = null!;
    bool _configurationWritten;
    bool _manifestWritten;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_project);
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "rules"));
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "skills", "example"));
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/example\"],\"languages\":[\"csharp\"]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/example\",\"availableTargets\":[\"example\"]}],\"engineeringProfiles\":[]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "rules", "general.md"), "# Rule");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "skills", "example", "SKILL.md"), "# Skill");
        _before = Fingerprint(_project);
    }

    void Because()
    {
        _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi"], ["cratis/example"], ["csharp"]), dryRun: true);
        _after = Fingerprint(_project);
        _configurationWritten = File.Exists(Path.Combine(_project, ".cratis", "ai.json"));
        _manifestWritten = File.Exists(Path.Combine(_project, ".cratis", "ai.manifest.json"));
        _performed = AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi"], ["cratis/example"], ["csharp"]));
    }

    /// <summary>Guards the comparisons below, which hold trivially on a run that planned nothing at all.</summary>
    [Fact] void should_report_the_changes_it_would_make() => _result.Actions.ShouldNotBeEmpty();

    [Fact] void should_report_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_leave_the_project_byte_for_byte_unchanged() => _after.ShouldEqual(_before);
    [Fact] void should_not_write_the_configuration() => _configurationWritten.ShouldBeFalse();
    [Fact] void should_not_write_the_manifest() => _manifestWritten.ShouldBeFalse();
    [Fact] void should_report_what_a_real_run_then_does() => _result.Actions.ShouldEqual(_performed.Actions);

    static IReadOnlyDictionary<string, string> Fingerprint(string root)
    {
        var fingerprints = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!Directory.Exists(root)) return fingerprints;
        foreach (var path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            fingerprints[relative] = Directory.Exists(path)
                ? "<directory>"
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
