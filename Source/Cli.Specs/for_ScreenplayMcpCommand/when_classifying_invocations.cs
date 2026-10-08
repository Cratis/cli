// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpCommand;

public class when_classifying_invocations : Specification
{
    [Fact] void should_classify_the_default_server_as_protocol() => ScreenplayMcpInvocation.IsProtocolRun(["screenplay", "mcp"]).ShouldBeTrue();
    [Fact] void should_classify_an_explicit_model_as_protocol() => ScreenplayMcpInvocation.IsProtocolRun(["screenplay", "mcp", "./models"]).ShouldBeTrue();
    [Fact] void should_classify_case_insensitive_names_as_protocol() => ScreenplayMcpInvocation.IsProtocolRun(["Screenplay", "MCP"]).ShouldBeTrue();
    [Fact] void should_exclude_long_help() => ScreenplayMcpInvocation.IsProtocolRun(["screenplay", "mcp", "--help"]).ShouldBeFalse();
    [Fact] void should_exclude_short_help() => ScreenplayMcpInvocation.IsProtocolRun(["screenplay", "mcp", "-h"]).ShouldBeFalse();
    [Fact] void should_exclude_question_mark_help() => ScreenplayMcpInvocation.IsProtocolRun(["screenplay", "mcp", "-?"]).ShouldBeFalse();
    [Fact] void should_exclude_help_after_a_path() => ScreenplayMcpInvocation.IsProtocolRun(["screenplay", "mcp", "./models", "--help"]).ShouldBeFalse();
    [Fact] void should_exclude_desktop_management() => ScreenplayMcpInvocation.IsProtocolRun(["screenplay", "desktop", "install"]).ShouldBeFalse();
    [Fact] void should_exclude_legacy_desktop_management() => ScreenplayMcpInvocation.IsProtocolRun(["screenplay", "mcp", "install"]).ShouldBeFalse();
    [Fact] void should_exclude_unrelated_commands() => ScreenplayMcpInvocation.IsProtocolRun(["chronicle", "events", "list"]).ShouldBeFalse();
    [Fact] void should_exclude_empty_arguments() => ScreenplayMcpInvocation.IsProtocolRun([]).ShouldBeFalse();
}
