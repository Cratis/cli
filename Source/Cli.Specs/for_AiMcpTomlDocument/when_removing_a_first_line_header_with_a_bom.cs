// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_AiMcpTomlDocument;

public class when_removing_a_first_line_header_with_a_bom : given.a_document
{
    void Establish()
    {
        File.WriteAllText(Path.Combine(_project, "config.toml"), "\uFEFF" + Owned + Following);
        _document = new(_project, "config.toml");
    }

    void Because()
    {
        _document.Set("mcp_servers", "screenplay", null);
        _document.Apply(AiFileOperations.Performing);
    }

    [Fact] void should_remove_the_whole_header_line_but_keep_the_bom() => Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(_project, "config.toml"))).ShouldEqual("\uFEFF" + Following);
    [Fact] void should_leave_valid_toml_with_the_unrelated_table() => _document.Contains("mcp_servers", "other").ShouldBeTrue();
}
