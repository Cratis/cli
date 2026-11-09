// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Studio;

/// <summary>
/// The exception that is thrown when Studio's MCP server refuses a request or a tool fails.
/// </summary>
/// <param name="message">What went wrong.</param>
public sealed class StudioMcpError(string message) : Exception(message);
