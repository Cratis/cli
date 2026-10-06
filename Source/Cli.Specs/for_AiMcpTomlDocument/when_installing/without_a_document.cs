// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiMcpTomlDocument.when_installing;

public class without_a_document : given.a_document
{
    JsonNode _value;

    void Establish()
    {
        _document = new(_project, "new-config.toml");
        _value = JsonNode.Parse("""{"command":"cratis","args":["screenplay","mcp"]}""")!;
    }

    void Because()
    {
        _document.Set("mcp_servers", "screenplay", _value);
        _document.Apply(AiFileOperations.Performing);
    }

    [Fact] void should_create_a_document_starting_directly_with_the_header() => File.ReadAllText(Path.Combine(_project, "new-config.toml")).StartsWith("[mcp_servers.screenplay]\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_valid_toml_with_the_server() => _document.Get("mcp_servers", "screenplay")!["command"]!.GetValue<string>().ShouldEqual("cratis");
}
