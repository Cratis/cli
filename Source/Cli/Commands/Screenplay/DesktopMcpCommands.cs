// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

internal delegate Task<int> DesktopMcpRun(string operation, string? clients, string? version, string? modelRoot, bool dryRun, TextWriter output, TextWriter error);

/// <summary>
/// Installs the Screenplay MCP server for desktop clients.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand("install", "Install the Screenplay MCP server in Claude Desktop and ChatGPT Desktop", Branch = typeof(ScreenplayBranch.Desktop))]
[CliExample("screenplay", "desktop", "install")]
[CliExample("screenplay", "desktop", "install", "--clients", "claude,chatgpt", "--model-root", "/absolute/path/to/specifications")]
[LlmDescription("Installs user-level Screenplay desktop integration, not project-local AI registrations. Noninteractive runs require --clients. Complete the host's trust/install dialog to enable the server.")]
[LlmOption("--clients", "string", "Select claude,chatgpt (one or both). Required for noninteractive runs.")]
[LlmOption("--version", "string", "Pin a Screenplay semantic release version without a leading v; otherwise use the latest stable release.")]
[LlmOption("--model-root", "string", "Existing physical model folder for ChatGPT. Claude asks for its folder in the install dialog.")]
[LlmOption("--dry-run", "bool", "Validate and preview without downloading artifacts, writing files, or opening a host. Release metadata may still be queried.")]
public sealed class InstallDesktopMcpCommand : AsyncCommand<DesktopMcpTargetSettings>
{
    readonly DesktopMcpRun _run;
    readonly TextWriter _output;
    readonly TextWriter _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallDesktopMcpCommand"/> class.
    /// </summary>
    public InstallDesktopMcpCommand() : this(DesktopMcp.Run, Console.Out, Console.Error)
    {
    }

    internal InstallDesktopMcpCommand(DesktopMcpRun run, TextWriter output, TextWriter error)
    {
        _run = run;
        _output = output;
        _error = error;
    }

    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, DesktopMcpTargetSettings settings, CancellationToken cancellationToken) =>
        DesktopMcp.Execute(context, settings, "install", settings.ModelRoot, settings.DryRun, _run, _output, _error);
}

/// <summary>
/// Updates the Screenplay MCP server for desktop clients.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand("update", "Update the Screenplay MCP server in Claude Desktop and ChatGPT Desktop", Branch = typeof(ScreenplayBranch.Desktop))]
[CliExample("screenplay", "desktop", "update", "--clients", "claude,chatgpt")]
[LlmDescription("Updates user-level Screenplay desktop integration while retaining the previous ChatGPT model folder when --model-root is omitted. Noninteractive runs require --clients. Host confirmation is still required.")]
[LlmOption("--clients", "string", "Select claude,chatgpt (one or both). Required for noninteractive runs.")]
[LlmOption("--version", "string", "Pin a Screenplay semantic release version without a leading v; otherwise use the latest stable release.")]
[LlmOption("--model-root", "string", "Existing physical model folder for ChatGPT. Retains the previous folder when omitted.")]
[LlmOption("--dry-run", "bool", "Validate and preview without downloading artifacts, writing files, or opening a host. Release metadata may still be queried.")]
public sealed class UpdateDesktopMcpCommand : AsyncCommand<DesktopMcpTargetSettings>
{
    readonly DesktopMcpRun _run;
    readonly TextWriter _output;
    readonly TextWriter _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDesktopMcpCommand"/> class.
    /// </summary>
    public UpdateDesktopMcpCommand() : this(DesktopMcp.Run, Console.Out, Console.Error)
    {
    }

    internal UpdateDesktopMcpCommand(DesktopMcpRun run, TextWriter output, TextWriter error)
    {
        _run = run;
        _output = output;
        _error = error;
    }

    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, DesktopMcpTargetSettings settings, CancellationToken cancellationToken) =>
        DesktopMcp.Execute(context, settings, "update", settings.ModelRoot, settings.DryRun, _run, _output, _error);
}

/// <summary>
/// Inspects the Screenplay desktop MCP distribution.
/// </summary>
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("status", "Inspect the Screenplay MCP server distribution in Claude Desktop and ChatGPT Desktop", Branch = typeof(ScreenplayBranch.Desktop))]
[CliExample("screenplay", "desktop", "status")]
[LlmDescription("Reports local desktop distribution state and available Screenplay updates without changing configuration. Registered sources and handed-off packages are not verified host installations. Use --version to avoid the latest-release lookup.")]
[LlmOption("--clients", "string", "Select claude,chatgpt (one or both). Defaults to both clients.")]
[LlmOption("--version", "string", "Screenplay semantic release version to compare against, without a leading v; otherwise check the latest stable release.")]
public sealed class DesktopMcpStatusCommand : AsyncCommand<DesktopMcpSettings>
{
    readonly DesktopMcpRun _run;
    readonly TextWriter _output;
    readonly TextWriter _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="DesktopMcpStatusCommand"/> class.
    /// </summary>
    public DesktopMcpStatusCommand() : this(DesktopMcp.Run, Console.Out, Console.Error)
    {
    }

    internal DesktopMcpStatusCommand(DesktopMcpRun run, TextWriter output, TextWriter error)
    {
        _run = run;
        _output = output;
        _error = error;
    }

    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, DesktopMcpSettings settings, CancellationToken cancellationToken) =>
        DesktopMcp.Execute(context, settings, "status", null, false, _run, _output, _error);
}

/// <summary>
/// Removes owned Screenplay desktop MCP integration.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand("uninstall", "Remove owned Screenplay MCP server integration from Claude Desktop and ChatGPT Desktop", Branch = typeof(ScreenplayBranch.Desktop))]
[CliExample("screenplay", "desktop", "uninstall", "--clients", "chatgpt")]
[LlmDescription("Removes only unchanged CLI-owned desktop integration, preserving user configuration and models. Noninteractive runs require --clients. Also disable/remove the extension or plugin in the desktop host.")]
[LlmOption("--clients", "string", "Select claude,chatgpt (one or both). Required for noninteractive runs.")]
[LlmOption("--version", "string", "Screenplay semantic release version without a leading v. Uninstall is local and does not query releases.")]
[LlmOption("--dry-run", "bool", "Validate and preview removal without writing files.")]
public sealed class UninstallDesktopMcpCommand : AsyncCommand<DesktopMcpRemovalSettings>
{
    readonly DesktopMcpRun _run;
    readonly TextWriter _output;
    readonly TextWriter _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="UninstallDesktopMcpCommand"/> class.
    /// </summary>
    public UninstallDesktopMcpCommand() : this(DesktopMcp.Run, Console.Out, Console.Error)
    {
    }

    internal UninstallDesktopMcpCommand(DesktopMcpRun run, TextWriter output, TextWriter error)
    {
        _run = run;
        _output = output;
        _error = error;
    }

    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, DesktopMcpRemovalSettings settings, CancellationToken cancellationToken) =>
        DesktopMcp.Execute(context, settings, "uninstall", null, settings.DryRun, _run, _output, _error);
}
