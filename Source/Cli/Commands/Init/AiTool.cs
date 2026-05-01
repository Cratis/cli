// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Init;

/// <summary>
/// Represents an AI tool that can be configured for Chronicle integration.
/// </summary>
public enum AiTool
{
    /// <summary>
    /// Claude Code (Anthropic).
    /// </summary>
    Claude = 0,

    /// <summary>
    /// GitHub Copilot.
    /// </summary>
    Copilot = 1,

    /// <summary>
    /// Cursor IDE.
    /// </summary>
    Cursor = 2,

    /// <summary>
    /// Windsurf IDE.
    /// </summary>
    Windsurf = 3,
}
