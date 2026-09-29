// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>Options for the Direct MCP bridge.</summary>
public sealed class DirectMcpSettings : CommandSettings
{
    /// <summary>Gets or sets the Direct origin to pin instead of the active login's.</summary>
    [CommandOption("--url <ORIGIN>")]
    [Description("Direct origin to pin (default: the active Direct login)")]
    public string? Url { get; set; }

    /// <summary>Gets or sets the tenant to pin instead of the active login's.</summary>
    [CommandOption("--tenant <TENANT>")]
    [Description("Tenant whose stored login to use (default: the active tenant on the active origin)")]
    public string? Tenant { get; set; }
}

/// <summary>Runs the stdio bridge to Direct's MCP server.</summary>
/// <remarks>
/// <c language="shell">cratis direct mcp</c> is normally dispatched before the interactive CLI starts (see <see cref="DirectMcpInvocation"/>);
/// this command covers invocations the early dispatch does not recognize.
/// </remarks>
[LlmDescription("Run a stdio MCP server that forwards JSON-RPC to Direct's remote MCP endpoint (<origin>/mcp) using the stored 'cratis direct login'. Origin, tenant and credential are pinned at startup. Meant to be launched by an MCP client; register it with 'cratis direct mcp install'. Tool calls act on Direct with your identity, and an expiring access token is refreshed, which rotates the stored refresh token.")]
[CommandEffect(CommandEffect.Mutating)]
[CliCommand("mcp", "Run the stdio bridge to Direct's MCP server", Branch = typeof(DirectBranch.Mcp), IsBranchDefault = true)]
[LlmOption("--url", "string", "Direct origin to pin (default: the active Direct login)")]
[LlmOption("--tenant", "string", "Tenant whose stored login to use")]
public sealed class DirectMcpCommand : AsyncCommand<DirectMcpSettings>
{
    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, DirectMcpSettings settings, CancellationToken cancellationToken)
    {
        var args = new List<string>();
        if (settings.Url is not null) args.AddRange(["--url", settings.Url]);
        if (settings.Tenant is not null) args.AddRange(["--tenant", settings.Tenant]);
        return DirectMcpInvocation.Run([.. args], new DirectMcpRunner(), Console.In, Console.Out, Console.Error, cancellationToken);
    }
}
