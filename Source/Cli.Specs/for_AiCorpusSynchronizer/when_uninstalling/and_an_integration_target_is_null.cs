// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_uninstalling;

public class and_an_integration_target_is_null : given.a_pi_corpus
{
    Exception? _error;
    string _sentinel = null!;

    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_project, ".cratis"));
        _sentinel = Path.Combine(_corpus, "sentinel.txt");
        File.WriteAllText(_sentinel, "keep");
        var path = System.Text.Json.JsonSerializer.Serialize(_sentinel);
        File.WriteAllText(Path.Combine(_project, ".cratis", "ai.manifest.json"),
            $$"""{"SourceRevision":"test","Files":[],"Integrations":[{"Path":{{path}},"Target":null,"IsDirectory":false}]}""");
    }

    void Because() => _error = Catch.Exception(() => AiCorpusSynchronizer.Uninstall(_project));

    [Fact] void should_reject_the_plan() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_name_the_offending_integration() => _error.Message.ShouldContain($"integration: {System.Text.Json.JsonSerializer.Serialize(_sentinel)}");
    [Fact] void should_preserve_the_unowned_file() => File.Exists(_sentinel).ShouldBeTrue();
}
