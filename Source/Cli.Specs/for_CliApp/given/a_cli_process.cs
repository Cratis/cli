// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliApp.given;

public class a_cli_process : for_ScreenplayMcpCommand.given.a_cli_process
{
    void Establish()
    {
        // Root-help specs choose their own process context rather than inheriting the agent running them.
        foreach (var name in new[] { "CLAUDECODE", "CLAUDE_CODE_ENTRYPOINT", "CURSOR_TRACE_DIR", "WINDSURF_SESSION_ID", "PI_CODING_AGENT", "PI_SESSION_ID", "TERM_PROGRAM" })
        {
            _environment[name] = null;
        }
    }
}
