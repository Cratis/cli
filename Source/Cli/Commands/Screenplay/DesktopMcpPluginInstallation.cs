// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>Records exactly the entry and package bytes owned by Cratis, not host installation state.</summary>
/// <param name="Version">Screenplay release.</param>
/// <param name="Folder">Owned home-relative package folder.</param>
/// <param name="ModelRoot">Explicit user-selected model, if any.</param>
/// <param name="Entry">Exact installed marketplace entry.</param>
/// <param name="Files">Owned relative paths and hashes, including directory markers.</param>
/// <param name="CleanupPending">Whether unregistering succeeded but package cleanup needs a retry.</param>
internal sealed record DesktopMcpPluginInstallation(string Version, string Folder, string? ModelRoot, JsonNode Entry, Dictionary<string, string> Files, bool CleanupPending = false);
