// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_force_is_requested : Specification
{
    string _project = null!;
    string _corpus = null!;
    SyncResult _result = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_corpus, "distribution"));
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "rules"));
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "skills", "example"));
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/example\"],\"languages\":[\"csharp\"]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/example\",\"availableTargets\":[\"example\"]}],\"engineeringProfiles\":[]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "rules", "general.md"), "# Rule");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "skills", "example", "SKILL.md"), "# Skill");
        AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi"], ["cratis/example"], ["csharp"]));
        File.AppendAllText(Path.Combine(_project, ".cratis", "ai", "skills", "example", "SKILL.md"), "\nUser addition");
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi"], ["cratis/example"], ["csharp"]), force: true);

    [Fact] void should_replace_the_modified_file() => File.ReadAllText(Path.Combine(_project, ".cratis", "ai", "skills", "example", "SKILL.md")).ShouldNotContain("User addition");
    [Fact] void should_not_report_a_conflict() => _result.Conflicts.ShouldBeEmpty();

    void Destroy()
    {
        Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
