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
    [Description("Tenant whose stored login to use (default: the active login's tenant when --url is not given, otherwise none)")]
    public string? Tenant { get; set; }

    /// <summary>Gets or sets whether to pin the bridge without a tenant.</summary>
    [CommandOption("--no-tenant")]
    [Description("Use a stored login without a tenant instead of the active tenant")]
    public bool NoTenant { get; set; }
}

/// <summary>Runs the stdio bridge to Direct's MCP server.</summary>
/// <remarks>
/// The CLI classifies the run as a protocol run (see <see cref="DirectMcpInvocation"/>), so no banner, hint or update check reaches the protocol stream
/// and framework diagnostics go to standard error.
/// </remarks>
[LlmDescription("Run a stdio MCP server that forwards JSON-RPC to Direct's remote MCP endpoint (<origin>/mcp) using the stored 'cratis direct login'. Origin, tenant and credential are pinned at startup. Meant to be launched by an MCP client; register it with 'cratis direct mcp install'. Tool calls act on Direct with your identity, and an expiring access token is refreshed, which rotates the stored refresh token.")]
[CommandEffect(CommandEffect.Mutating)]
[CliCommand("mcp", "Run the stdio bridge to Direct's MCP server", Branch = typeof(DirectBranch.Mcp), IsBranchDefault = true)]
[LlmOption("--url", "string", "Direct origin to pin (default: the active Direct login)")]
[LlmOption("--tenant", "string", "Tenant whose stored login to use (default: the active login's tenant when --url is not given, otherwise none)")]
[LlmOption("--no-tenant", "bool", "Use a stored login without a tenant instead of the active tenant")]
public sealed class DirectMcpCommand : AsyncCommand<DirectMcpSettings>
{
    readonly IDirectMcpRunner _runner;
    readonly TextReader _input;
    readonly TextWriter _output;
    readonly TextWriter _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectMcpCommand"/> class.
    /// </summary>
    public DirectMcpCommand()
        : this(new DirectMcpRunner(), Console.In, Console.Out, Console.Error)
    {
    }

    internal DirectMcpCommand(IDirectMcpRunner runner, TextReader input, TextWriter output, TextWriter error)
    {
        _runner = runner;
        _input = input;
        _output = output;
        _error = error;
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, DirectMcpSettings settings, CancellationToken cancellationToken)
    {
        if (context.Remaining.Parsed.Count > 0 || context.Remaining.Raw.Count > 0)
        {
            var option = context.Remaining.Raw.Count > 0 ? context.Remaining.Raw[0] : context.Remaining.Parsed.First().Key;
            await _error.WriteLineAsync($"Unknown option '{option}'. Run 'cratis direct mcp --help' for usage.");
            await _error.WriteLineAsync(DirectMcpInvocation.Usage);
            return ExitCodes.ValidationError;
        }

        return await DirectMcpInvocation.Run(settings, _runner, _input, _output, _error, cancellationToken);
    }
}
