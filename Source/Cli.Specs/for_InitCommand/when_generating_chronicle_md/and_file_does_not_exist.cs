// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_generating_chronicle_md;

public class and_file_does_not_exist : Specification
{
    string _content;

    void Because() => _content = ChronicleDocGenerator.Generate();

    [Fact] void should_contain_chronicle_heading() => _content.ShouldContain("# Chronicle CLI Reference");
    [Fact] void should_contain_connection_setup() => _content.ShouldContain("## Connection Setup");
    [Fact] void should_contain_troubleshooting() => _content.ShouldContain("## Troubleshooting Decision Tree");
    [Fact] void should_contain_command_overview() => _content.ShouldContain("## Command Overview");
    [Fact] void should_contain_quiet_flag() => _content.ShouldContain("--quiet");
    [Fact] void should_contain_yes_flag() => _content.ShouldContain("--yes");
}
