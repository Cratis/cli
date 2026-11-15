// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Commands.Extension;

/// <summary>
/// Settings for restoring extensions.
/// </summary>
public class RestoreExtensionSettings : ExtensionSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the lock must already pin every reference.
    /// </summary>
    [CommandOption("--locked")]
    [Description("Fail instead of resolving a reference the lock does not pin, and never change screenplay.lock. Use in CI.")]
    public bool Locked { get; set; }
}

/// <summary>
/// Fills the extension cache from screenplay.lock.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand("restore", "Restore the extensions a model references from screenplay.lock", Branch = typeof(ExtensionBranch))]
[CliExample("extension", "restore")]
[CliExample("extension", "restore", "--locked")]
[CliExample("extension", "restore", "--source", "./vendor")]
[LlmDescription("Fills the digest-keyed extension cache from screenplay.lock, verifying each package's digest, contract hash, signature and publisher against the project's allow-list in .cratis/extensions.json. A reference the lock does not pin is resolved (latest, or the latest in its range) and pinned, unless --locked is passed, which fails instead and never changes the lock. Registry credentials come from cratis config, never from the model or the lock, and are never printed. Exit codes: 0 restored, 1 unresolvable, 2 registry unreachable, 4 credentials refused, 5 unlocked, unallowed, unsigned or mismatched.")]
[LlmOption("--locked", "bool", "Fail on a reference the lock does not pin; never write the lock.")]
[LlmOption("--root", "string", "Model root. Defaults to the current directory, honoring mcpServers.screenplay.root in .cratis/ai.json.")]
[LlmOption("--source", "string", "A vendored folder or oci://host/prefix mirror to fetch from instead of the registries the references name.")]
[LlmOutputAdvice("json-compact", "Per-reference outcomes with status, version, digest, publisher and failure kind; plain text by default.")]
public class RestoreExtensionCommand : AsyncCommand<RestoreExtensionSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, RestoreExtensionSettings settings, CancellationToken cancellationToken) =>
        ExtensionCommandSupport.Run(settings, (runtime, token) => runtime.Service.Restore(runtime.Workspace, settings.Locked, token), Render, cancellationToken);

    static void Render(ExtensionReport report)
    {
        foreach (var outcome in report.Outcomes) ExtensionCommandSupport.WriteOutcome(outcome);
        if (report.LockChanged) Console.WriteLine($"Wrote {ExtensionLock.FileName}.");
    }
}
