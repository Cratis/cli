// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// The exception that is thrown when a folder other than the one the Screenplay MCP server would serve holds an interrupted write.
/// </summary>
/// <param name="selected">The folder the server would serve.</param>
/// <param name="pending">The other folders holding <c language="shell">.screenplay/pending.json</c>.</param>
public sealed class ScreenplayWorkspaceStateConflict(string selected, IReadOnlyCollection<string> pending)
    : Exception($"PendingOperation: '{selected}' would be served, but {string.Join(", ", pending.Select(path => $"'{path}'"))} hold{(pending.Count == 1 ? "s" : string.Empty)} an interrupted write in .screenplay/pending.json. Start the server with that folder as the path and recover its write first.");
