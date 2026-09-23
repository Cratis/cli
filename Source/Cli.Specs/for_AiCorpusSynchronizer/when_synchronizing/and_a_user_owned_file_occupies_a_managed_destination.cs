// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_user_owned_file_occupies_a_managed_destination : Specification
{
    string _project = null!;
    string _corpus = null!;
    SyncResult _result = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_project, ".cratis", "ai", "rules"));
        Directory.CreateDirectory(Path.Combine(_corpus, "distribution"));
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "rules"));
        File.WriteAllText(Path.Combine(_project, ".cratis", "ai", "rules", "general.md"), "# User rule");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/example\"],\"languages\":[\"csharp\"]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/example\"}],\"engineeringProfiles\":[]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "rules", "general.md"), "# Cratis rule");
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi"], ["cratis/example"], ["csharp"]), force: true);

    [Fact] void should_not_overwrite_the_user_file_even_when_forced() => File.ReadAllText(Path.Combine(_project, ".cratis", "ai", "rules", "general.md")).ShouldEqual("# User rule");
    [Fact] void should_report_the_collision() => _result.Conflicts.ShouldContain("rules/general.md");
    [Fact] void should_not_change_other_files() => _result.Actions.ShouldBeEmpty();

    void Destroy()
    {
        Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
