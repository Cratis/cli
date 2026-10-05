// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_DesktopMcp;

public class when_updating_a_personal_plugin : given.a_personal_marketplace
{
    string _update;
    async Task Establish()
    {
        await _client.Install(_artifact, "4.55.0", _home, false);
        _update = Package("4.56.0");
    }
    async Task Because() => _result = await _client.Install(_update, "4.56.0", null, false);

    [Fact] void should_replace_the_owned_source_version() => _client.ReadOwned()!.Version.ShouldEqual("4.56.0");
    [Fact] void should_retain_the_selected_model() => _client.ReadOwned()!.ModelRoot.ShouldEqual(_home);
    [Fact] void should_not_duplicate_marketplace_entries() => JsonNode.Parse(File.ReadAllText(_marketplace))!["plugins"]!.AsArray().Count.ShouldEqual(2);
}
