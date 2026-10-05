// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_DesktopMcp;

public class when_updating_modified_marketplace_content : given.a_personal_marketplace
{
    string _before;
    string _update;
    async Task Establish()
    {
        await _client.Install(_artifact, "4.55.0", _home, false);
        var marketplace = JsonNode.Parse(await File.ReadAllTextAsync(_marketplace))!;
        marketplace["plugins"]!.AsArray().Single(entry => entry!["name"]!.GetValue<string>() == "cratis-screenplay")!["category"] = "User edited";
        _before = marketplace.ToJsonString();
        await File.WriteAllTextAsync(_marketplace, _before);
        _update = Package("4.56.0");
    }
    async Task Because() => _error = await Catch.Exception(() => _client.Update(_update, "4.56.0", null, false));

    [Fact] void should_refuse_the_modified_registration() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_preserve_user_edits() => File.ReadAllText(_marketplace).ShouldEqual(_before);
    [Fact] void should_keep_the_previous_ownership_record() => _client.ReadOwned()!.Version.ShouldEqual("4.55.0");
}
