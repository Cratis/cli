// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tomlyn;
using Tomlyn.Model;

namespace Cratis.Cli.for_AiMcpRegistration.when_uninstalling;

public class with_codex_toml : given.a_screenplay_corpus
{
    const string Original = "# user header\r\nmodel = 'my-model'\r\n[mcp_servers.other] # keep comment\r\ncommand = 'other-tool'\r\n";
    const string Following = "\n# user-owned following table\n[other]\nsetting = 'keep'\n";
    string _after;

    void Establish()
    {
        _configuration = _configuration with { Harnesses = ["codex"] };
        Write(".codex/config.toml", Original);
        Install();
        File.AppendAllText(ProjectFile(".codex/config.toml"), Following);
    }

    void Because()
    {
        _result = AiCorpusSynchronizer.Uninstall(_project);
        _after = File.ReadAllText(ProjectFile(".codex/config.toml"));
    }

    [Fact] void should_keep_every_original_byte() => _after.StartsWith(Original, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_following_table_and_comment_bytes() => _after.EndsWith(Following, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_remove_only_the_owned_table() => ((TomlTable)TomlSerializer.Deserialize<TomlTable>(_after)!["mcp_servers"]).ContainsKey("screenplay").ShouldBeFalse();
    [Fact] void should_not_report_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
