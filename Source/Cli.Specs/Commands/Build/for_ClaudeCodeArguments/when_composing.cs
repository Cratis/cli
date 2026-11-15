// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_ClaudeCodeArguments;

public class when_composing : Specification
{
    IReadOnlyList<string> _arguments;
    string _configuration;
    IReadOnlyList<string> _proposing;

    void Because()
    {
        var session = given.a_session.With();
        _arguments = ClaudeCodeArguments.For(session, "/tmp/system.md", "/tmp/mcp.json", null, null);
        _configuration = ClaudeCodeArguments.McpConfiguration(session.Tools).ToJsonString();
        _proposing = ClaudeCodeArguments.AllowedTools(given.a_session.With(mayChangeTools: false));
    }

    [Fact] void should_refuse_what_is_not_allowed_without_asking() => _arguments.ShouldContain("dontAsk");
    [Fact] void should_never_switch_permission_checks_off() => _arguments.Any(argument => argument.Contains("bypass", StringComparison.OrdinalIgnoreCase) || argument.Contains("skip-permissions", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_leave_the_user_s_own_settings_out() =>
        _arguments.SkipWhile(argument => argument != "--setting-sources").Skip(1).First().ShouldEqual("project,local");
    [Fact] void should_use_only_the_session_servers() => _arguments.ShouldContain("--strict-mcp-config");
    [Fact] void should_allow_writing_permitted_paths_by_absolute_rule() => _arguments.ShouldContain($"Write(/{Path.GetFullPath(given.a_session.Project)}/src/**)");
    [Fact] void should_allow_a_permitted_command() => _arguments.ShouldContain("Bash(npm test:*)");
    [Fact] void should_allow_writing_the_outcome() => _arguments.ShouldContain($"Write(/{Path.Combine(Path.GetFullPath(given.a_session.Workspace), "outcome.json")})");
    [Fact] void should_not_write_the_studio_token_into_the_configuration() => _configuration.ShouldNotContain(given.a_session.StudioToken);
    [Fact] void should_reference_the_studio_token_from_the_environment() => _configuration.ShouldContain("${CRATIS_BUILD_MCP_TOKEN_STUDIO}");
    [Fact] void should_serve_screenplay_tools_from_the_cli() => _configuration.ShouldContain("\"screenplay\",\"mcp\"");
    [Fact] void should_allow_every_studio_tool_when_it_may_change() => _arguments.ShouldContain("mcp__studio");
    [Fact] void should_allow_only_reading_studio_tools_while_proposing() => _proposing.ShouldNotContain("mcp__studio");
    [Fact] void should_allow_reading_the_board_while_proposing() => _proposing.ShouldContain("mcp__studio__studio_get_event_model");
}
