// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tomlyn;
using Tomlyn.Model;

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_codex_bom : given.a_screenplay_corpus
{
    const string Original = "\uFEFF# user comment\r\nmodel = 'other'\r\n[mcp_servers.another]\r\ncommand = 'keep'\r\n";
    byte[] _before;
    string _after;

    void Establish()
    {
        _configuration = _configuration with { Harnesses = ["codex"] };
        Write(".codex/config.toml", Original);
        _before = File.ReadAllBytes(ProjectFile(".codex/config.toml"));
    }

    void Because()
    {
        _result = Install();
        _after = File.ReadAllText(ProjectFile(".codex/config.toml"));
    }

    [Fact] void should_preserve_the_original_bom_and_unrelated_bytes() => File.ReadAllBytes(ProjectFile(".codex/config.toml")).AsSpan(0, _before.Length).SequenceEqual(_before).ShouldBeTrue();
    [Fact] void should_keep_another_server_and_comments() => _after.Contains("# user comment\r\nmodel = 'other'\r\n[mcp_servers.another]", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_register_the_server() => ((TomlTable)((TomlTable)TomlSerializer.Deserialize<TomlTable>(_after.TrimStart('\uFEFF'))!["mcp_servers"])["screenplay"])["command"].ShouldEqual("cratis");
    [Fact] void should_report_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
