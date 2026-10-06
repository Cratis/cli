// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpTomlDocument;

public class when_removing_a_table_with_inline_comments : given.a_document
{
    void Establish()
    {
        File.WriteAllText(Path.Combine(_project, "config.toml"), Original + "[mcp_servers.screenplay] # table\r\n  command = 'old' # command\r\n  args = [] # arguments\r\n" + Following);
        _document = new(_project, "config.toml");
    }

    void Because()
    {
        _document.Set("mcp_servers", "screenplay", null);
        _document.Apply(AiFileOperations.Performing);
    }

    [Fact] void should_preserve_comments_and_their_surrounding_trivia_exactly() => Content().ShouldEqual(Original + " # table\r\n   # command\r\n   # arguments\r\n" + Following);
}
