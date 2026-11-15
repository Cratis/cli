// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;

namespace Cratis.Cli.Commands.Extension;

/// <summary>
/// Settings for vendoring extensions.
/// </summary>
public class VendorExtensionSettings : ExtensionSettings
{
    /// <summary>
    /// Gets or sets where to copy the packages to.
    /// </summary>
    [CommandArgument(0, "<DESTINATION>")]
    [Description("A folder, or oci://host/prefix for a private registry. The model's own 'extensions' folder is the one the compiler reads without a restore.")]
    public string Destination { get; set; } = string.Empty;
}

/// <summary>
/// Copies every locked extension package to a folder or a private registry.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand("vendor", "Copy every locked extension package to a folder or private registry", Branch = typeof(ExtensionBranch))]
[CliExample("extension", "vendor", "./extensions")]
[CliExample("extension", "vendor", "oci://registry.internal/mirror")]
[LlmDescription("Copies every package screenplay.lock pins, with its signature, to a local folder or an OCI registry you control, after verifying digest, contract hash, signature and publisher. Restore can then be pointed at the copy with --source for air-gapped deployments. A reference the lock does not pin fails; restore first. A folder holds <digest>/contract.extension, package.json and the artifact parts. npm packages vendor to folders only. Exit codes: 0 vendored, 1 unresolvable, 2 registry unreachable, 4 credentials refused, 5 unlocked, unallowed, unsigned or mismatched.")]
[LlmOption("<DESTINATION>", "string", "Folder or oci://host/prefix to copy to.")]
[LlmOption("--root", "string", "Model root. Defaults to the current directory, honoring mcpServers.screenplay.root in .cratis/ai.json.")]
[LlmOption("--source", "string", "A vendored folder or oci://host/prefix mirror to fetch from instead of the registries the references name.")]
[LlmOutputAdvice("json-compact", "Per-reference outcomes with status, version, digest and publisher; plain text by default.")]
public class VendorExtensionCommand : AsyncCommand<VendorExtensionSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, VendorExtensionSettings settings, CancellationToken cancellationToken) =>
        ExtensionCommandSupport.Run(settings, (runtime, token) => runtime.Service.Vendor(runtime.Workspace, ExtensionCommandSupport.OpenDestination(settings.Destination, runtime.Http), token), Render, cancellationToken);

    static void Render(ExtensionReport report)
    {
        foreach (var outcome in report.Outcomes) ExtensionCommandSupport.WriteOutcome(outcome);
    }
}
