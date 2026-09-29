// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>The origin and tenant a Direct MCP bridge is asked to pin; null selects the active Direct login.</summary>
/// <param name="Url">An explicit Direct origin.</param>
/// <param name="Tenant">An explicit tenant.</param>
internal sealed record DirectMcpOptions(string? Url, string? Tenant);
