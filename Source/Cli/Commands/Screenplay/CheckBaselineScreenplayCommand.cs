// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Checks released event revisions without changing source or identity state.
/// </summary>
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("check", "Check released event revisions against the release baseline", Branch = typeof(ScreenplayBranch.Baseline))]
[CliExample("screenplay", "baseline", "check")]
[CliExample("screenplay", "baseline", "check", "./Model", "-o", "json-compact")]
[LlmDescription("Checks the model's released event revisions using persisted .screenplay/identities.json and its release baseline. Never reconciles or writes identities. Exit codes: 0 clean, 1 violations, 2 unreadable or refused; CLI usage errors use -1 (255 on POSIX). These differ from the general CLI exit codes.")]
[LlmOption("[ROOT]", "string", "Model root. Defaults to the current directory, honoring mcpServers.screenplay.root in .cratis/ai.json.")]
[LlmOutputAdvice("json-compact", "Structured violations, failureKind, reason, admittedEntryCount, baselineRevision and exitCode; plain text by default.")]
public class CheckBaselineScreenplayCommand : Command<BaselineScreenplaySettings>
{
    /// <inheritdoc/>
    public override int Execute(CommandContext context, BaselineScreenplaySettings settings, CancellationToken cancellationToken) =>
        ScreenplayReleaseBaseline.Execute(context, settings, ReleaseBaselineCommands.Check);
}
