// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_agents_md_is_user_owned : Specification
{
    string _project = null!;
    string _corpus = null!;
    SyncResult _result = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_project);
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "rules"));
        File.WriteAllText(Path.Combine(_project, "AGENTS.md"), "Read the project-owned instructions.");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/example\"],\"languages\":[\"csharp\"]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/example\"}],\"engineeringProfiles\":[]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "rules", "general.md"), "# General rule");
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi"], ["cratis/example"], ["csharp"]));

    [Fact] void should_preserve_the_project_owned_instructions() => File.ReadAllText(Path.Combine(_project, "AGENTS.md")).ShouldEqual("Read the project-owned instructions.");
    [Fact] void should_not_report_a_conflict() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_still_install_the_managed_rules() => File.Exists(Path.Combine(_project, ".cratis", "ai", "rules", "general.md")).ShouldBeTrue();

    void Destroy()
    {
        Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
