// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_only_typescript_is_selected : Specification
{
    string _project = null!;
    string _corpus = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_corpus, "distribution"));
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "rules"));
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "skills", "csharp-skill"));
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "skills", "typescript-skill"));
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/example\"],\"languages\":[\"csharp\",\"typescript\"]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/example\",\"composes\":[\"cratis/example/csharp\",\"cratis/example/typescript\"]},{\"id\":\"cratis/example/csharp\",\"languages\":[\"csharp\"],\"availableTargets\":[\"csharp-skill\"]},{\"id\":\"cratis/example/typescript\",\"languages\":[\"typescript\"],\"availableTargets\":[\"typescript-skill\"]}],\"engineeringProfiles\":[]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "rules", "general.md"), "# Rule");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "skills", "csharp-skill", "SKILL.md"), "# CSharp");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "skills", "typescript-skill", "SKILL.md"), "# TypeScript");
    }

    void Because() => AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi"], ["cratis/example"], ["typescript"]));

    [Fact] void should_install_the_typescript_skill() => File.Exists(Path.Combine(_project, ".cratis", "ai", "skills", "typescript-skill", "SKILL.md")).ShouldBeTrue();
    [Fact] void should_not_install_the_csharp_skill() => File.Exists(Path.Combine(_project, ".cratis", "ai", "skills", "csharp-skill", "SKILL.md")).ShouldBeFalse();

    void Destroy()
    {
        Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
