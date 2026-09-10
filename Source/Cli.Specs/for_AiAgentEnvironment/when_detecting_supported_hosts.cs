// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiAgentEnvironment;

public class when_detecting_supported_hosts : Specification
{
    [Fact] void should_detect_claude_code() => Detect("CLAUDECODE", "1").ShouldBeTrue();
    [Fact] void should_not_assume_a_vscode_terminal_is_an_agent_process() => Detect("VSCODE_PID", "123").ShouldBeFalse();
    [Fact] void should_detect_cursor() => Detect("TERM_PROGRAM", "cursor").ShouldBeTrue();
    [Fact] void should_detect_windsurf() => Detect("WINDSURF_SESSION_ID", "session").ShouldBeTrue();
    [Fact] void should_detect_pi() => Detect("PI_CODING_AGENT", "1").ShouldBeTrue();
    [Fact] void should_not_detect_an_unknown_host() => Detect("UNKNOWN_AGENT", "1").ShouldBeFalse();

    static bool Detect(string variable, string value) =>
        AiAgentEnvironment.IsDetected(name => name == variable ? value : null);
}
