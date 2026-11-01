// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand;

public class when_classifying_invocations : Specification
{
    [Fact] void should_classify_the_default_bridge_as_protocol() => DirectMcpInvocation.IsProtocolRun(["direct", "mcp"]).ShouldBeTrue();
    [Fact] void should_classify_a_pinned_bridge_as_protocol() => DirectMcpInvocation.IsProtocolRun(["direct", "mcp", "--url", "https://direct.example", "--tenant", "team"]).ShouldBeTrue();
    [Fact] void should_classify_a_bridge_without_a_tenant_as_protocol() => DirectMcpInvocation.IsProtocolRun(["direct", "mcp", "--no-tenant"]).ShouldBeTrue();
    [Fact] void should_classify_case_insensitive_names_as_protocol() => DirectMcpInvocation.IsProtocolRun(["Direct", "MCP"]).ShouldBeTrue();
    [Fact] void should_exclude_the_install_subcommand() => DirectMcpInvocation.IsProtocolRun(["direct", "mcp", "install", "--client", "claude"]).ShouldBeFalse();
    [Fact] void should_exclude_the_status_subcommand() => DirectMcpInvocation.IsProtocolRun(["direct", "mcp", "status"]).ShouldBeFalse();
    [Fact] void should_exclude_the_uninstall_subcommand() => DirectMcpInvocation.IsProtocolRun(["direct", "mcp", "uninstall"]).ShouldBeFalse();
    [Fact] void should_exclude_long_help() => DirectMcpInvocation.IsProtocolRun(["direct", "mcp", "--help"]).ShouldBeFalse();
    [Fact] void should_exclude_short_help() => DirectMcpInvocation.IsProtocolRun(["direct", "mcp", "-h"]).ShouldBeFalse();
    [Fact] void should_exclude_question_mark_help() => DirectMcpInvocation.IsProtocolRun(["direct", "mcp", "-?"]).ShouldBeFalse();
    [Fact] void should_exclude_help_after_options() => DirectMcpInvocation.IsProtocolRun(["direct", "mcp", "--tenant", "team", "--help"]).ShouldBeFalse();
    [Fact] void should_exclude_other_direct_commands() => DirectMcpInvocation.IsProtocolRun(["direct", "login"]).ShouldBeFalse();
    [Fact] void should_exclude_unrelated_commands() => DirectMcpInvocation.IsProtocolRun(["chronicle", "events", "list"]).ShouldBeFalse();
    [Fact] void should_exclude_empty_arguments() => DirectMcpInvocation.IsProtocolRun([]).ShouldBeFalse();
}
