// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Which board, under what name and in which mode a modeling session connects.
/// </summary>
/// <param name="EventModelId">The event model board.</param>
/// <param name="Name">What the agent is called and mentioned by on the board.</param>
/// <param name="Unattended">Whether the agent acts without asking.</param>
public sealed partial record ModelingSessionOptions(string EventModelId, string Name, bool Unattended)
{
    /// <summary>
    /// Gets the mode Studio connects the agent in.
    /// </summary>
    public string Mode => Unattended ? "Unattended" : "ProposeAndConfirm";

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9 ._-]{0,59}$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex AgentName { get; }

    [GeneratedRegex("event-model/(?<id>[0-9a-fA-F-]{36})", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex BoardAddress { get; }

    /// <summary>
    /// Checks a name against the rule Studio applies to the names of connected agents: letters, digits, spaces, dots,
    /// hyphens and underscores, starting with a letter or digit, at most 60 characters.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns><see langword="true"/> when Studio accepts it.</returns>
    public static bool IsValidName(string name) => AgentName.IsMatch(name);

    /// <summary>
    /// Reads the board's identifier from either the identifier itself or the board's address in Studio.
    /// </summary>
    /// <param name="board">The identifier or the address.</param>
    /// <returns>The identifier, or <see langword="null"/> when the value is neither.</returns>
    public static string? BoardFrom(string? board)
    {
        if (string.IsNullOrWhiteSpace(board)) return null;
        if (Guid.TryParse(board, out var id)) return id.ToString();
        var match = BoardAddress.Match(board);
        return match.Success && Guid.TryParse(match.Groups["id"].Value, out var addressed) ? addressed.ToString() : null;
    }
}
