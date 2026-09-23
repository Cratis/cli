// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// The exception that is thrown when MCP configuration cannot be used safely.
/// </summary>
/// <param name="message">The invalid configuration and required correction.</param>
public sealed class AiMcpConfigurationInvalid(string message) : Exception(message);
