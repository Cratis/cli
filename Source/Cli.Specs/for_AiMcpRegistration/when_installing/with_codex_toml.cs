// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tomlyn;
using Tomlyn.Model;

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_codex_toml : given.a_screenplay_corpus
{
    const string Original = "# user header\r\nmodel = 'my-model'\r\n[mcp_servers.other] # keep comment\r\ncommand = 'other-tool'\r\nargs = ['blå', '[mcp_servers.screenplay]']\r\n";
    string _after;

    void Establish()
    {
        _configuration = _configuration with { Harnesses = ["codex"] };
        Write(".codex/config.toml", Original);
    }

    void Because()
    {
        _result = Install();
        _after = File.ReadAllText(ProjectFile(".codex/config.toml"));
    }

    [Fact] void should_preserve_every_existing_byte() => _after.StartsWith(Original, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_create_a_real_native_table() => ((TomlTable)((TomlTable)TomlSerializer.Deserialize<TomlTable>(_after)!["mcp_servers"])["screenplay"])["command"].ShouldEqual("cratis");
    [Fact] void should_use_the_native_runtime_directory_without_inventing_relative_cwd_resolution() => _after.Contains("cwd", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_report_unsupported_adapter() => _result.UnsupportedMcpServers!.ShouldBeEmpty();
}
