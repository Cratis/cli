// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli;

/// <summary>
/// Routes protocol invocations without initializing the interactive CLI or its update network request.
/// </summary>
internal static class CliEntryPoint
{
    internal static Task<int> Run(string[] args, Func<Task<int>> interactive, TextReader input, TextWriter output, TextWriter error, IDirectMcpRunner direct) =>
        DirectMcpInvocation.IsMatch(args)
            ? DirectMcpInvocation.Run(args[2..], direct, input, output, error, CancellationToken.None)
            : interactive();
}
