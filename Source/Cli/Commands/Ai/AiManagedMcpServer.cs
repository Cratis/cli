// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Ownership of one native server entry, never of a harness configuration file.
/// </summary>
/// <param name="Harness">The native harness adapter.</param>
/// <param name="Path">The project-relative configuration path.</param>
/// <param name="Collection">The native server collection property or table.</param>
/// <param name="Id">The server member name.</param>
/// <param name="Installed">The last installed semantic JSON value.</param>
/// <param name="Preimage">The prior member; null records an absent member, not permission to adopt a foreign one.</param>
public sealed record AiManagedMcpServer(string Harness, string Path, string Collection, string Id, JsonObject Installed, JsonNode? Preimage = null);
