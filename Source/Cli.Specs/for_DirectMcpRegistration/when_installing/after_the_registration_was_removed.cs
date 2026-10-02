// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class after_the_registration_was_removed : given.a_home_and_a_project
{
    void Establish()
    {
        Install(DirectMcpScope.User, ["claude"]);
        File.WriteAllText(HomeFile(".claude.json"), "{\"mcpServers\":{\"other\":{\"command\":\"other\"}}}");
    }

    void Because() => _plan = Install(DirectMcpScope.User, ["claude"]);

    [Fact] void should_not_report_a_conflict() => _plan.Conflicts.ShouldBeEmpty();
    [Fact] void should_add_it_again() => _plan.Changes.Single().Action.ShouldEqual("add");
    [Fact] void should_write_the_registration() => ReadJson(HomeFile(".claude.json"))["mcpServers"]!["cratis-direct"]!["command"]!.GetValue<string>().ShouldEqual("cratis");
    [Fact] void should_keep_the_other_server() => ReadJson(HomeFile(".claude.json"))["mcpServers"]!["other"].ShouldNotBeNull();
    [Fact] void should_still_own_it() => DirectMcpManifest.Read(_home).Servers.Single().Harness.ShouldEqual("claude");
}
