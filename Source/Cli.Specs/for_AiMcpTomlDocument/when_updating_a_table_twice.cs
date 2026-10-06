// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiMcpTomlDocument;

public class when_updating_a_table_twice : given.a_document
{
    JsonNode _value;
    string _before;

    void Establish()
    {
        _value = JsonNode.Parse("""{"command":"cratis","args":["screenplay","mcp"]}""")!;
        _document.Set("mcp_servers", "screenplay", _value);
        _document.Apply(AiFileOperations.Performing);
        _before = Content();
        _document = new(_project, "config.toml");
    }

    void Because()
    {
        _document.Set("mcp_servers", "screenplay", _value);
        _document.Apply(AiFileOperations.Performing);
    }

    [Fact] void should_produce_identical_bytes_on_the_second_update() => Content().ShouldEqual(_before);
    [Fact] void should_keep_every_unrelated_byte() => Content().StartsWith(Original + Following, StringComparison.Ordinal).ShouldBeTrue();
}
