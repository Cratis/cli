// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// The usable and skipped MCP server descriptors read from the corpus.
/// </summary>
/// <param name="Servers">Descriptors that are complete and supported.</param>
/// <param name="Skipped">Entries that cannot be registered, with the reason.</param>
/// <param name="Notice">A message about the whole file (such as an unsupported schema version), or <see langword="null"/>.</param>
internal sealed record AiMcpCatalog(IReadOnlyList<AiMcpDescriptor> Servers, IReadOnlyList<AiMcpSkipped> Skipped, string? Notice)
{
    internal static AiMcpCatalog Empty { get; } = new([], [], null);
}

/// <summary>
/// An MCP server entry that is skipped rather than registered.
/// </summary>
/// <param name="Id">The entry id, or an empty string when absent.</param>
/// <param name="Profiles">The profiles that would select the entry.</param>
/// <param name="Reason">Why the entry is skipped.</param>
internal sealed record AiMcpSkipped(string Id, IReadOnlyList<string> Profiles, string Reason);
