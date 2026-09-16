// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_composed_profile_brings_in_application_rules : Specification
{
    string _project = null!;
    string _corpus = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var rules = Path.Combine(_corpus, ".cratis", "ai", "rules");
        Directory.CreateDirectory(rules);
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/full/csharp\"],\"languages\":[\"csharp\"]}");
        File.WriteAllText(
            Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"),
            "{\"publicProfiles\":[" +
            "{\"id\":\"cratis/full/csharp\",\"composes\":[\"cratis/application/csharp\",\"cratis/documentation\"]}," +
            "{\"id\":\"cratis/application/csharp\",\"languages\":[\"csharp\"]}," +
            "{\"id\":\"cratis/documentation\",\"languages\":[\"language-agnostic\"]}]," +
            "\"engineeringProfiles\":[]}");
        File.WriteAllText(Path.Combine(rules, "general.md"), "# General");
        File.WriteAllText(Path.Combine(rules, "vertical-slices.md"), "---\napplyTo: \"**/*.cs\"\nprofile: application\n---\n# Vertical slices");
        File.WriteAllText(Path.Combine(rules, "documentation.md"), "---\napplyTo: \"**/Documentation/**/*.{md,mdx}\"\n---\n# Documentation");
        File.WriteAllText(Path.Combine(rules, "framework.md"), "---\napplyTo: \"**/*\"\nprofile: framework\n---\n# Framework");
    }

    void Because() => AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi"], ["cratis/full/csharp"], ["csharp"]));

    [Fact] void should_install_application_rules_reached_through_composition() => File.Exists(Path.Combine(_project, ".cratis", "ai", "rules", "vertical-slices.md")).ShouldBeTrue();
    [Fact] void should_install_documentation_rules_reached_through_composition() => File.Exists(Path.Combine(_project, ".cratis", "ai", "rules", "documentation.md")).ShouldBeTrue();
    [Fact] void should_install_general_rules() => File.Exists(Path.Combine(_project, ".cratis", "ai", "rules", "general.md")).ShouldBeTrue();
    [Fact] void should_not_install_framework_rules() => File.Exists(Path.Combine(_project, ".cratis", "ai", "rules", "framework.md")).ShouldBeFalse();

    void Destroy()
    {
        Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
