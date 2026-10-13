// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Admits released event revisions into the local release baseline.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand("admit", "Admit released event revisions into the release baseline", Branch = typeof(ScreenplayBranch.Baseline))]
[CliExample("screenplay", "baseline", "admit", "./Model", "--release", "v1.0.0")]
[CliExample("screenplay", "baseline", "admit", "./Model", "--release", "v1.0.0", "--from-esm", "./shipped.esm.json")]
[LlmDescription("Monotonically admits released event revisions and atomically writes the local release baseline under Screenplay's recovery journal. Requires persisted .screenplay/identities.json and --release. Exit codes: 0 admitted, 1 violations, 2 unreadable or refused; CLI usage errors use -1 (255 on POSIX). These differ from the general CLI exit codes.")]
[LlmOption("[ROOT]", "string", "Model root. Defaults to the current directory, honoring mcpServers.screenplay.root in .cratis/ai.json.")]
[LlmOption("--release", "string", "Required release label for admission.")]
[LlmOption("--from-esm", "string", "Optional canonical shipped ESM file; otherwise admit the current model.")]
[LlmOutputAdvice("json-compact", "Structured violations, failureKind, reason, admittedEntryCount, baselineRevision and exitCode; plain text by default.")]
public class AdmitBaselineScreenplayCommand : Command<AdmitBaselineScreenplaySettings>
{
    /// <inheritdoc/>
    public override int Execute(CommandContext context, AdmitBaselineScreenplaySettings settings, CancellationToken cancellationToken) =>
        ScreenplayReleaseBaseline.Execute(context, settings, root => ReleaseBaselineCommands.Admit(root, settings.Release!, settings.FromEsm));
}
