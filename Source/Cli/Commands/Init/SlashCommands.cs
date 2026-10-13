// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Init;

/// <summary>
/// Provides slash command / prompt templates for AI tools.
/// </summary>
public static class SlashCommands
{
    /// <summary>
    /// The chronicle-diagnose slash command content for Claude Code.
    /// </summary>
#pragma warning disable MA0136 // Raw string trailing newline is intentional for markdown files
    public const string ChronicleDiagnose = """
# Chronicle Diagnose

Run a diagnostic check on the connected Chronicle server.

## Steps

1. Run `cratis version -o json` to check connectivity and version compatibility.
2. Run `cratis chronicle observers list -o plain` to check observer states.
3. Run `cratis chronicle failed-partitions list -o plain` to find failing partitions.
4. If there are failed partitions, run `cratis chronicle failed-partitions show <observer-id> <partition> -o json` for each to get error details.
5. Run `cratis chronicle recommendations list -o plain` to check for pending recommendations.

## Output

Summarize findings:
- Server connectivity and version compatibility
- Number of observers and their states
- Any failed partitions with error summaries
- Any pending recommendations
- Suggested remediation steps
""";
#pragma warning restore MA0136
}
