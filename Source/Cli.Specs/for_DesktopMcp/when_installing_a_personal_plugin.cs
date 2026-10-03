// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_DesktopMcp;

public class when_installing_a_personal_plugin : given.a_personal_marketplace
{
    async Task Because() => _result = await _client.Install(_artifact, "4.55.0", _home, false);

    [Fact] void should_register_only_one_screenplay_source() => JsonNode.Parse(File.ReadAllText(_marketplace))!["plugins"]!.AsArray().Count.ShouldEqual(2);
    [Fact] void should_preserve_the_marketplace_name() => JsonNode.Parse(File.ReadAllText(_marketplace))!["name"]!.GetValue<string>().ShouldEqual("my-marketplace");
    [Fact] void should_preserve_foreign_entry_formatting() => File.ReadAllText(_marketplace).ShouldContain("{ \"name\": \"foreign\", \"source\": { \"source\": \"local\", \"path\": \"./mine\" } }");
    [Fact] void should_preserve_foreign_top_level_bytes() => File.ReadAllText(_marketplace).ShouldContain("  \"extra\": { \"keep\": true },");
    [Fact] void should_require_host_confirmation() => _result.ShouldContain("No host installation is claimed");
    [Fact] void should_record_the_selected_model() => _client.ReadOwned()!.ModelRoot.ShouldEqual(_home);
}
