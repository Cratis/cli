// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Commands.Extension;

/// <summary>
/// Settings for updating extensions.
/// </summary>
public class UpdateExtensionSettings : ExtensionSettings
{
    /// <summary>
    /// Gets or sets the system to update.
    /// </summary>
    [CommandArgument(0, "[SYSTEM]")]
    [Description("The system whose extension to update. Updates every referenced extension when omitted.")]
    public string? System { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to report without writing the lock.
    /// </summary>
    [CommandOption("--dry-run")]
    [Description("Report what would change and how each contract differs, without writing screenplay.lock.")]
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to write the lock although a new contract breaks uses in the model.
    /// </summary>
    [CommandOption("--allow-incompatible")]
    [Description("Write the lock even when a new contract no longer offers what the model uses. The uses are reported as errors by the compiler afterwards.")]
    public bool AllowIncompatible { get; set; }
}

/// <summary>
/// Moves extension references to the newest release in their range.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand("update", "Update extension references to the newest release in their range", Branch = typeof(ExtensionBranch))]
[CliExample("extension", "update")]
[CliExample("extension", "update", "Payments", "--dry-run")]
[LlmDescription("Moves one system's extension, or every referenced extension, to the newest release within its version range (the newest release when it names none), verifying signature and publisher first. Before writing screenplay.lock it reports how each contract changed (added and removed members) and every use in the model the new contract no longer offers; a reference with incompatible uses is left as it was unless --allow-incompatible is passed. --dry-run writes nothing. Exit codes: 0 updated or unchanged, 1 unresolvable, 2 registry unreachable, 4 credentials refused, 5 unallowed, unsigned, mismatched or incompatible.")]
[LlmOption("[SYSTEM]", "string", "System to update; every referenced extension when omitted.")]
[LlmOption("--dry-run", "bool", "Report without writing the lock.")]
[LlmOption("--allow-incompatible", "bool", "Write the lock although new contracts break uses in the model.")]
[LlmOption("--root", "string", "Model root. Defaults to the current directory, honoring mcpServers.screenplay.root in .cratis/ai.json.")]
[LlmOption("--source", "string", "A vendored folder or oci://host/prefix mirror to resolve from.")]
[LlmOutputAdvice("json-compact", "Per-reference outcomes and the contract changes with added, removed and incompatibleUses; plain text by default.")]
public class UpdateExtensionCommand : AsyncCommand<UpdateExtensionSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, UpdateExtensionSettings settings, CancellationToken cancellationToken) =>
        ExtensionCommandSupport.Run(settings, (runtime, token) => runtime.Service.Update(runtime.Workspace, settings.System, settings.DryRun, settings.AllowIncompatible, token), Render, cancellationToken);

    static void Render(ExtensionReport report)
    {
        foreach (var outcome in report.Outcomes) ExtensionCommandSupport.WriteOutcome(outcome);
        foreach (var change in report.Changes)
        {
            Console.WriteLine($"{change.Reference}: {change.From ?? "(not locked)"} -> {change.To}");
            foreach (var added in change.Added) Console.WriteLine($"  + {added}");
            foreach (var removed in change.Removed) Console.WriteLine($"  - {removed}");
            foreach (var use in change.IncompatibleUses) Console.WriteLine($"  ! {use}");
        }

        if (report.LockChanged) Console.WriteLine($"Wrote {ExtensionLock.FileName}.");
    }
}
