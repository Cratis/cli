// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>Runs the Direct MCP bridge for resolved options.</summary>
internal interface IDirectMcpRunner
{
    Task Run(DirectMcpOptions options, TextReader input, TextWriter output, TextWriter log, CancellationToken cancellationToken);
}
