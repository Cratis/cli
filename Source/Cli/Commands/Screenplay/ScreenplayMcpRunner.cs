// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.Commands.Screenplay;

internal sealed class ScreenplayMcpRunner : IScreenplayMcpRunner
{
    public void Run(string root, TextReader input, TextWriter output) => ScreenplayMcpServer.Run(root, input, output);
}
