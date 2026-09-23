// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

public class with_a_modified_codex_entry : given.a_screenplay_corpus
{
    string _changed;

    void Establish()
    {
        _configuration = _configuration with { Harnesses = ["codex"] };
        Install();
        _changed = File.ReadAllText(ProjectFile(".codex/config.toml")).Replace("command = \"cratis\"", "command = \"user-tool\"", StringComparison.Ordinal);
        Write(".codex/config.toml", _changed);
    }

    void Because() => _result = Install(force: true);

    [Fact] void should_report_native_table_drift() => _result.Conflicts.Single().ShouldContain("modified owned MCP entry");
    [Fact] void should_not_overwrite_the_edited_table() => File.ReadAllText(ProjectFile(".codex/config.toml")).ShouldEqual(_changed);
}
