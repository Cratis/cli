// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CompletionsCommand;

public class when_completing_from_an_installed_ai_manifest : Specification
{
    string _project;
    IReadOnlyList<string> _result;

    void Establish()
    {
        _project = Path.Combine(Directory.GetCurrentDirectory(), ".ai-work", $"completion-spec-{Guid.NewGuid():N}");
        var corpus = Path.Combine(_project, ".cratis", "ai");
        Directory.CreateDirectory(corpus);
        File.WriteAllText(Path.Combine(corpus, "manifest.json"), """
            {"profiles":["cratis/documentation","cratis/engineering/csharp","cratis/custom"],"harnesses":["pi","claude"],"languages":["csharp","typescript"]}
            """);
    }

    void Because() => _result = OfflineCompletion.Candidates("ai-profiles", "cratis/", _project);

    void Destroy() => Directory.Delete(_project, recursive: true);

    [Fact] void should_use_the_local_catalog_instead_of_only_the_curated_list() => _result.ShouldContain("cratis/custom");
    [Fact] void should_find_three_installed_profiles() => _result.Count.ShouldEqual(3);
}
