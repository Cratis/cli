// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiMcpTomlDocument.when_removing_an_installed_table;

public class without_a_terminal_newline : given.a_document
{
    const string Before = "a = 1";

    void Establish()
    {
        File.WriteAllText(Path.Combine(_project, "config.toml"), Before);
        _document = new(_project, "config.toml");
        _document.Set("mcp_servers", "screenplay", JsonNode.Parse("""{"command":"cratis","args":["screenplay","mcp"]}"""));
        _document.Apply(AiFileOperations.Performing);
        _document = new(_project, "config.toml");
    }

    void Because()
    {
        _document.Set("mcp_servers", "screenplay", null);
        _document.Apply(AiFileOperations.Performing);
    }

    [Fact] void should_restore_the_original_unterminated_line_exactly() => Content().ShouldEqual(Before);
    [Fact] void should_leave_valid_toml_without_the_owned_table() => _document.Contains("mcp_servers", "screenplay").ShouldBeFalse();
}
