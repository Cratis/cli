// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

internal interface IScreenplayMcpRunner
{
    void Run(string root, TextReader input, TextWriter output);
}
