// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiMcpTomlDocument;

public class when_updating_an_indented_table : given.a_document
{
    JsonNode _value;

    void Establish() => _value = JsonNode.Parse("""{"command":"cratis","args":["screenplay","mcp"]}""")!;
    void Because()
    {
        _document.Set("mcp_servers", "screenplay", _value);
        _document.Apply(AiFileOperations.Performing);
    }

    [Fact] void should_preserve_every_unrelated_byte() => Content().StartsWith(Original + Following, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_leave_trailing_whitespace() => Content().Split('\n').Any(line => line.TrimEnd('\r').EndsWith(' ') || line.TrimEnd('\r').EndsWith('\t')).ShouldBeFalse();
    [Fact] void should_leave_valid_toml_with_the_updated_server() => _document.Get("mcp_servers", "screenplay")!["command"]!.GetValue<string>().ShouldEqual("cratis");
}
