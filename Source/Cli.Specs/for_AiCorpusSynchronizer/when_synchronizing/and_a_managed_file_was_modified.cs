// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_managed_file_was_modified : Specification
{
    string _project = null!;
    string _corpus = null!;
    SyncResult _result = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_corpus, "distribution"));
        Directory.CreateDirectory(Path.Combine(_corpus, ".ai", "rules"));
        Directory.CreateDirectory(Path.Combine(_corpus, ".ai", "skills", "example"));
        File.WriteAllText(Path.Combine(_corpus, ".ai", "manifest.json"), "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/example\"],\"languages\":[\"csharp\"]}");
        File.WriteAllText(Path.Combine(_corpus, "distribution", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/example\",\"availableTargets\":[\"example\"]}],\"engineeringProfiles\":[]}");
        File.WriteAllText(Path.Combine(_corpus, ".ai", "rules", "general.md"), "# Rule");
        File.WriteAllText(Path.Combine(_corpus, ".ai", "skills", "example", "SKILL.md"), "# Skill");
        AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi"], ["cratis/example"], ["csharp"]));
        File.AppendAllText(Path.Combine(_project, ".ai", "skills", "example", "SKILL.md"), "\nUser addition");
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi"], ["cratis/example"], ["csharp"]));

    [Fact] void should_report_the_conflict() => _result.Conflicts.ShouldContain("skills/example/SKILL.md");
    [Fact] void should_preserve_the_user_change() => File.ReadAllText(Path.Combine(_project, ".ai", "skills", "example", "SKILL.md")).ShouldContain("User addition");
    [Fact] void should_mark_installed_text_as_cratis_managed() => File.ReadAllText(Path.Combine(_project, ".ai", "rules", "general.md")).ShouldContain("cratis-ai-managed");

    void Destroy()
    {
        Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
