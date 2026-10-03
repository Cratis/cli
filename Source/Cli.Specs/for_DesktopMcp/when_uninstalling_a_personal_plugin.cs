// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_DesktopMcp;

public class when_uninstalling_a_personal_plugin : given.a_personal_marketplace
{
    string _folder;
    async Task Establish()
    {
        await _client.Install(_artifact, "4.55.0", _home, false);
        _folder = Path.Combine(_home, _client.ReadOwned()!.Folder);
    }
    async Task Because() => _result = await _client.Uninstall(false);

    [Fact] void should_leave_only_the_foreign_plugin() => JsonNode.Parse(File.ReadAllText(_marketplace))!["plugins"]!.AsArray().Single()!["name"]!.GetValue<string>().ShouldEqual("foreign");
    [Fact] void should_remove_the_active_owned_source() => Directory.Exists(_folder).ShouldBeFalse();
    [Fact] void should_forget_ownership() => _client.ReadOwned().ShouldBeNull();
    [Fact] void should_direct_host_removal() => _result.ShouldContain("Disable/remove Screenplay in ChatGPT Plugins");
}
