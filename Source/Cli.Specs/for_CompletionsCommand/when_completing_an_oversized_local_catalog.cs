// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Cli.for_CompletionsCommand;

public class when_completing_an_oversized_local_catalog : Specification
{
    string _project;
    IReadOnlyList<string> _result;

    void Establish()
    {
        _project = Path.Combine(Directory.GetCurrentDirectory(), ".ai-work", $"completion-spec-{Guid.NewGuid():N}");
        var corpus = Path.Combine(_project, ".cratis", "ai");
        Directory.CreateDirectory(corpus);
        var profiles = Enumerable.Range(0, 100).Select(index => $"cratis/profile-{index:D3}-{new string('a', 100)}")
            .Append("cratis/unsafe;rm -rf /").ToArray();
        File.WriteAllText(Path.Combine(corpus, "manifest.json"), JsonSerializer.Serialize(new { profiles }));
    }

    void Because() => _result = OfflineCompletion.Candidates("ai-profiles", "cratis/", _project);

    void Destroy() => Directory.Delete(_project, recursive: true);

    [Fact] void should_bound_the_number_of_results() => (_result.Count <= 128 && _result.Count > 0).ShouldBeTrue();
    [Fact] void should_bound_output_bytes() => (Encoding.UTF8.GetByteCount(string.Join('\n', _result)) <= 8192).ShouldBeTrue();
    [Fact] void should_not_emit_untrusted_shell_tokens() => _result.ShouldNotContain("cratis/unsafe;rm -rf /");
}
