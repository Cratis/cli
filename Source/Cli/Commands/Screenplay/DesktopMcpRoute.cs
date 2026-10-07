// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Rewrites legacy desktop management routes without representing command help or parsing options.
/// </summary>
internal static class DesktopMcpRoute
{
    internal static string[] Normalize(string[] args) => Normalize(args, Console.Error);

    internal static string[] Normalize(string[] args, TextWriter error)
    {
        if (args.Length < 3 ||
            !string.Equals(args[0], "screenplay", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(args[1], "mcp", StringComparison.OrdinalIgnoreCase) ||
            !new[] { "install", "status", "update", "uninstall" }.Contains(args[2], StringComparer.Ordinal))
        {
            return args;
        }

        var normalized = (string[])args.Clone();
        normalized[1] = "desktop";
        error.WriteLine($"'cratis screenplay mcp {args[2]}' is deprecated; use 'cratis screenplay desktop {args[2]}' instead.");
        return normalized;
    }
}
