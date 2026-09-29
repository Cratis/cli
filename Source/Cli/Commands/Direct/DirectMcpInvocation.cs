// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>
/// Starts the Direct MCP bridge before the interactive CLI, so no banner, hint or update check reaches the protocol stream.
/// </summary>
internal static class DirectMcpInvocation
{
    internal const string Usage = "Usage: cratis direct mcp [--url <ORIGIN>] [--tenant <TENANT>]";

    /// <summary>
    /// Matches <c language="shell">direct mcp</c> with only options. Subcommands such as <c language="shell">install</c> and help requests go to the
    /// interactive CLI.
    /// </summary>
    /// <param name="args">The command line.</param>
    /// <returns>True when the bridge should start.</returns>
    internal static bool IsMatch(string[] args) => args.Length >= 2 &&
        string.Equals(args[0], "direct", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(args[1], "mcp", StringComparison.OrdinalIgnoreCase) &&
        (args.Length == 2 || args[2].StartsWith('-')) &&
        !args.Skip(2).Any(IsHelp);

    internal static async Task<int> Run(string[] args, IDirectMcpRunner runner, TextReader input, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        try
        {
            await runner.Run(Parse(args), input, output, error, cancellationToken);
            return ExitCodes.Success;
        }
        catch (DirectMcpUsage)
        {
            await error.WriteLineAsync(Usage);
            return ExitCodes.ValidationError;
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

    static bool IsHelp(string arg) =>
        string.Equals(arg, "--help", StringComparison.Ordinal) || string.Equals(arg, "-h", StringComparison.Ordinal) || string.Equals(arg, "-?", StringComparison.Ordinal);

    static DirectMcpOptions Parse(string[] args)
    {
        string? url = null;
        string? tenant = null;
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
            {
                throw new DirectMcpUsage();
            }

            switch (args[index])
            {
                case "--url" when url is null:
                    url = args[index + 1];
                    break;
                case "--tenant" when tenant is null:
                    tenant = args[index + 1];
                    break;
                default:
                    throw new DirectMcpUsage();
            }
        }

        return new(url, tenant);
    }

    sealed class DirectMcpUsage : Exception;
}
