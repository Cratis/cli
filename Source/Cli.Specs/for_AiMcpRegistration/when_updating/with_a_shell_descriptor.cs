// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

public class with_a_shell_descriptor : given.a_screenplay_corpus
{
    readonly string[] _hostPaths = [".mcp.json", ".vscode/mcp.json", ".cursor/mcp.json", ".codex/config.toml", "opencode.json", ".cratis/ai.manifest.json"];
    Dictionary<string, string> _before;
    Exception _error;

    void Establish()
    {
        Install();
        _before = _hostPaths.ToDictionary(path => path, path => File.ReadAllText(ProjectFile(path)));
        var descriptor = Path.Combine(_corpus, ".cratis/ai/mcp-servers.json");
        File.WriteAllText(descriptor, File.ReadAllText(descriptor).Replace("\"command\":\"cratis\"", "\"command\":\"/bin/sh\"", StringComparison.Ordinal));
    }

    void Because() => _error = Catch.Exception(() => Install());

    [Fact] void should_reject_the_untrusted_update() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_preserve_every_client_entry_and_the_manifest()
    {
        foreach (var path in _hostPaths) File.ReadAllText(ProjectFile(path)).ShouldEqual(_before[path]);
    }
}
