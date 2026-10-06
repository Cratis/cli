// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiMcpTomlDocument.when_installing;

public class with_only_a_bom : given.a_document
{
    JsonNode _value;

    void Establish()
    {
        File.WriteAllText(Path.Combine(_project, "config.toml"), "\uFEFF");
        _document = new(_project, "config.toml");
        _value = JsonNode.Parse("""{"command":"cratis","args":["screenplay","mcp"]}""")!;
    }

    void Because()
    {
        _document.Set("mcp_servers", "screenplay", _value);
        _document.Apply(AiFileOperations.Performing);
    }

    [Fact] void should_keep_the_bom_directly_before_the_header() => Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(_project, "config.toml"))).StartsWith("\uFEFF[mcp_servers.screenplay]\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_valid_toml_with_the_server() => _document.Get("mcp_servers", "screenplay")!["command"]!.GetValue<string>().ShouldEqual("cratis");
}
