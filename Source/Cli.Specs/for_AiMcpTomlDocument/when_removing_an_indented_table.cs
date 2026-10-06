// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tomlyn;
using Tomlyn.Model;

namespace Cratis.Cli.for_AiMcpTomlDocument;

public class when_removing_an_indented_table : given.a_document
{
    void Because()
    {
        _document.Set("mcp_servers", "screenplay", null);
        _document.Apply(AiFileOperations.Performing);
    }

    [Fact] void should_remove_owned_lines_without_leaving_indentation_behind() => Content().ShouldEqual(Original + Following);
    [Fact] void should_leave_valid_toml_without_the_owned_table() => ((TomlTable)TomlSerializer.Deserialize<TomlTable>(Content())!["mcp_servers"]).ContainsKey("screenplay").ShouldBeFalse();
}
