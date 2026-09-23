// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;

namespace Cratis.Cli;

/// <summary>
/// Routes protocol invocations without initializing the interactive CLI or its update network request.
/// </summary>
internal static class CliEntryPoint
{
    internal static Task<int> Run(string[] args, Func<Task<int>> interactive, IScreenplayMcpRunner runner, TextReader input, TextWriter output, TextWriter error, string workingDirectory, Func<string, string?> environment) =>
        ScreenplayMcpInvocation.IsMatch(args)
            ? Task.FromResult(ScreenplayMcpInvocation.Run(args[2..], runner, input, output, error, workingDirectory, environment))
            : interactive();
}
