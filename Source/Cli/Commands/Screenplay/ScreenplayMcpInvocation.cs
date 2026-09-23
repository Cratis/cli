// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Handles protocol startup before the interactive CLI, update checks, and diagnostic rendering.
/// </summary>
internal static class ScreenplayMcpInvocation
{
    const string Usage = "Usage: cratis screenplay mcp [path] | --project-root <directory> | --project-root-env <variable>";

    internal static bool IsMatch(string[] args) => args.Length >= 2 &&
        string.Equals(args[0], "screenplay", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(args[1], "mcp", StringComparison.OrdinalIgnoreCase);

    internal static int Run(string[] args, IScreenplayMcpRunner runner, TextReader input, TextWriter output, TextWriter error, string workingDirectory, Func<string, string?> environment)
    {
        try
        {
            if (args is ["--help"] or ["-h"])
            {
                error.WriteLine(Usage);
                return ExitCodes.Success;
            }
            var (path, project, variable) = Parse(args);
            var root = ScreenplayMcpRoot.Resolve(path, project, variable, workingDirectory, environment);
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

    static (string? Path, string? Project, string? Variable) Parse(string[] args) => args switch
    {
        [] => (null, null, null),
        ["--project-root", var value] when !string.IsNullOrWhiteSpace(value) => (null, value, null),
        ["--project-root-env", var value] when !string.IsNullOrWhiteSpace(value) => (null, null, value),
        [var value] when !value.StartsWith('-') => (value, null, null),
        _ => throw new AiMcpConfigurationInvalid(Usage)
    };
}
