// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesMarkdown;

public class when_converting_to_markup : Specification
{
    string _result;

    void Because() => _result = ReleaseNotesMarkdown.ToMarkup("## Added\n\n- Use `cratis [x]` and **bold** [docs](https://x.y/z)\n```\nraw [code]\n```");

    [Fact] void should_render_headings_bold() => _result.ShouldContain("Added[/]");
    [Fact] void should_drop_the_heading_marker() => _result.ShouldNotContain("##");
    [Fact] void should_render_bullets() => _result.ShouldContain("\u2022");
    [Fact] void should_escape_brackets_in_code() => _result.ShouldContain("cratis [[x]]");
    [Fact] void should_render_bold() => _result.ShouldContain("[bold]bold[/]");
    [Fact] void should_show_link_targets() => _result.ShouldContain("docs [");
    [Fact] void should_escape_code_blocks() => _result.ShouldContain("raw [[code]]");
    [Fact] void should_drop_fences() => _result.ShouldNotContain("```");
    [Fact] void should_produce_valid_markup() => new Spectre.Console.Markup(_result).ShouldNotBeNull();
}
