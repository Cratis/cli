// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class after_installing : given.a_home_and_a_project
{
    void Establish()
    {
        Write(HomeFile(".claude.json"), "{\"mcpServers\":{\"other\":{\"command\":\"other\"}}}");
        Write(HomeFile(".codex/config.toml"), "model = \"o3\"\n");
        Install(DirectMcpScope.User, ["claude", "codex"]);
    }

    void Because() => _plan = Uninstall(DirectMcpScope.User);

    [Fact] void should_remove_the_claude_registration() => ReadJson(HomeFile(".claude.json"))["mcpServers"]!.AsObject().ContainsKey("cratis-direct").ShouldBeFalse();
    [Fact] void should_keep_other_claude_servers() => ReadJson(HomeFile(".claude.json"))["mcpServers"]!["other"].ShouldNotBeNull();
    [Fact] void should_remove_the_codex_table() => File.ReadAllText(HomeFile(".codex/config.toml")).ShouldNotContain("cratis-direct");
    [Fact] void should_keep_other_codex_settings() => File.ReadAllText(HomeFile(".codex/config.toml")).ShouldContain("model = \"o3\"");
    [Fact] void should_report_each_removal() => _plan.Changes.Select(change => change.Action).ShouldContainOnly("remove", "remove");
    [Fact] void should_drop_the_ownership_record() => File.Exists(HomeFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
