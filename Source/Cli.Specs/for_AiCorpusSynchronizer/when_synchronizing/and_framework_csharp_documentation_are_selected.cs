// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_framework_csharp_documentation_are_selected : Specification
{
    string _project = null!;
    string _corpus = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var rules = Path.Combine(_corpus, ".cratis", "ai", "rules");
        Directory.CreateDirectory(rules);
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/engineering/csharp\",\"cratis/documentation\"],\"languages\":[\"csharp\",\"typescript\"]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/documentation\"}],\"engineeringProfiles\":[{\"id\":\"cratis/engineering/csharp\"}]}");
        File.WriteAllText(Path.Combine(rules, "general.md"), "# General");
        File.WriteAllText(Path.Combine(rules, "csharp.md"), "---\napplyTo: \"**/*.cs\"\n---\n# CSharp");
        File.WriteAllText(Path.Combine(rules, "typescript.md"), "---\napplyTo: \"**/*.ts,**/*.tsx\"\n---\n# TypeScript");
        File.WriteAllText(Path.Combine(rules, "framework.md"), "---\napplyTo: \"**/*\"\nprofile: framework\n---\n# Framework");
        File.WriteAllText(Path.Combine(rules, "vertical-slices.md"), "---\napplyTo: \"**/*.cs\"\nprofile: application\n---\n# Vertical slices");
        File.WriteAllText(Path.Combine(rules, "documentation.md"), "---\napplyTo: \"**/Documentation/**/*.{md,mdx}\"\n---\n# Documentation");
    }

    void Because() => AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi"], ["cratis/engineering/csharp", "cratis/documentation"], ["csharp"]));

    [Fact] void should_install_general_rules() => File.Exists(Path.Combine(_project, ".cratis", "ai", "rules", "general.md")).ShouldBeTrue();
    [Fact] void should_install_csharp_rules() => File.Exists(Path.Combine(_project, ".cratis", "ai", "rules", "csharp.md")).ShouldBeTrue();
    [Fact] void should_install_framework_rules() => File.Exists(Path.Combine(_project, ".cratis", "ai", "rules", "framework.md")).ShouldBeTrue();
    [Fact] void should_install_documentation_rules() => File.Exists(Path.Combine(_project, ".cratis", "ai", "rules", "documentation.md")).ShouldBeTrue();
    [Fact] void should_not_install_typescript_rules() => File.Exists(Path.Combine(_project, ".cratis", "ai", "rules", "typescript.md")).ShouldBeFalse();
    [Fact] void should_not_install_application_rules() => File.Exists(Path.Combine(_project, ".cratis", "ai", "rules", "vertical-slices.md")).ShouldBeFalse();

    void Destroy()
    {
        Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
