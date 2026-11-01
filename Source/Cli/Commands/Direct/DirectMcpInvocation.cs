// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>
/// Classifies Direct MCP protocol runs and starts the bridge with bound settings, so no banner, hint or update check reaches the protocol stream.
/// </summary>
internal static class DirectMcpInvocation
{
    internal const string Usage = "Usage: cratis direct mcp [--url <ORIGIN>] [--tenant <TENANT> | --no-tenant]";

    /// <summary>
    /// Matches <c language="shell">direct mcp</c> with only options. Subcommands such as <c language="shell">install</c> and help requests are
    /// ordinary CLI runs.
    /// </summary>
    /// <param name="args">The command line.</param>
    /// <returns>True when the bridge will run and standard output carries only JSON-RPC.</returns>
    internal static bool IsProtocolRun(string[] args) => args.Length >= 2 &&
        string.Equals(args[0], "direct", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(args[1], "mcp", StringComparison.OrdinalIgnoreCase) &&
        (args.Length == 2 || args[2].StartsWith('-')) &&
        !args.Skip(2).Any(IsHelp);

    internal static async Task<int> Run(DirectMcpSettings settings, IDirectMcpRunner runner, TextReader input, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        if (!IsValid(settings))
        {
            await error.WriteLineAsync(Usage);
            return ExitCodes.ValidationError;
        }

        try
        {
            await runner.Run(new(settings.Url, settings.Tenant, settings.NoTenant), input, output, error, cancellationToken);
            return ExitCodes.Success;
        }
        catch (DirectAuthError ex)
        {
            // No Spectre rendering, output envelopes, banners or stack traces on the protocol stream.
            await error.WriteLineAsync($"Direct MCP: {ex.Message}");
            return ExitCodes.AuthenticationError;
        }
        catch (Exception ex) when (DirectLoginFlow.IsSafeFailure(ex))
        {
            await error.WriteLineAsync("Direct MCP: the bridge stopped because Direct, the credential store or the CLI configuration could not be used.");
            return ExitCodes.ConnectionError;
        }
    }

    static bool IsValid(DirectMcpSettings settings) =>
        !(settings.NoTenant && settings.Tenant is not null) &&
        (settings.Url is null || !string.IsNullOrWhiteSpace(settings.Url)) &&
        (settings.Tenant is null || !string.IsNullOrWhiteSpace(settings.Tenant));

    static bool IsHelp(string arg) =>
        string.Equals(arg, "--help", StringComparison.Ordinal) || string.Equals(arg, "-h", StringComparison.Ordinal) || string.Equals(arg, "-?", StringComparison.Ordinal);
}
