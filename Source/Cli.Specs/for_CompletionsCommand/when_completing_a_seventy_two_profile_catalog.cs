// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_CompletionsCommand;

public class when_completing_a_seventy_two_profile_catalog : Specification
{
    string _project;
    IReadOnlyList<string> _result;

    void Establish()
    {
        _project = Path.Combine(Directory.GetCurrentDirectory(), ".ai-work", $"completion-spec-{Guid.NewGuid():N}");
        var corpus = Path.Combine(_project, ".cratis", "ai");
        Directory.CreateDirectory(corpus);
        var profiles = Enumerable.Range(0, 72).Select(index => $"cratis/profile-{index:D3}").ToArray();
        File.WriteAllText(Path.Combine(corpus, "manifest.json"), JsonSerializer.Serialize(new { profiles }));
    }

    void Because() => _result = OfflineCompletion.Candidates("ai-profiles", "cratis/", _project);

    void Destroy() => Directory.Delete(_project, recursive: true);

    [Fact] void should_offer_every_profile_in_the_current_catalog_size() => _result.Count.ShouldEqual(72);
    [Fact] void should_include_the_final_profile() => _result.ShouldContain("cratis/profile-071");
}
