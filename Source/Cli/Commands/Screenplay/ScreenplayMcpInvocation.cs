// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Classifies protocol runs and starts the embedded server with bound settings.
/// </summary>
internal static class ScreenplayMcpInvocation
{
    internal static bool IsProtocolRun(string[] args) => args.Length >= 2 &&
        string.Equals(args[0], "screenplay", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(args[1], "mcp", StringComparison.OrdinalIgnoreCase) &&
        !(args.Length >= 3 && new[] { "install", "status", "update", "uninstall" }.Contains(args[2], StringComparer.Ordinal)) &&
        !args.Skip(2).Any(arg => new[] { "--help", "-h", "-?" }.Contains(arg, StringComparer.Ordinal));

    internal static int Run(string? path, string? projectRoot, string? projectRootEnvironment, IScreenplayMcpRunner runner, TextReader input, TextWriter output, TextWriter error, string workingDirectory, Func<string, string?> environment)
    {
        try
        {
            var root = ScreenplayMcpRoot.Resolve(path, projectRoot, projectRootEnvironment, workingDirectory, environment, message => error.WriteLine($"Screenplay MCP: {message}"));
            runner.Run(root, input, output);
            return ExitCodes.Success;
        }
        catch (Exception exception)
        {
            // No Spectre rendering, output-format envelopes, banners, or stack traces on the protocol stream.
            error.WriteLine($"Screenplay MCP: {exception.Message}");
            return ExitCodes.ValidationError;
        }
    }
}
