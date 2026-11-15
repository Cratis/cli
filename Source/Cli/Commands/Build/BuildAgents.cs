// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The coding agents a builder runs, by name: Claude Code natively through its own command line, the others over the Agent
/// Client Protocol through the command that starts their ACP server. Any ACP agent runs with <c language="csharp">--agent-command</c>.
/// </summary>
public static class BuildAgents
{
    /// <summary>The name of Claude Code.</summary>
    public const string Claude = "claude";

    /// <summary>The name of an agent started only from <c language="csharp">--agent-command</c>.</summary>
    public const string Acp = "acp";

    static readonly Dictionary<string, (string Name, string Command)> _acpCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        ["codex"] = ("Codex", "npx -y @zed-industries/codex-acp"),
        ["gemini"] = ("Gemini CLI", "gemini --experimental-acp"),
        ["copilot"] = ("Copilot CLI", "copilot --acp"),
        ["opencode"] = ("OpenCode", "opencode acp"),
        ["cursor"] = ("Cursor", string.Empty),
        [Acp] = ("The agent", string.Empty)
    };

    /// <summary>
    /// Gets the names of every agent this host knows.
    /// </summary>
    public static IEnumerable<string> Names => [Claude, .. _acpCommands.Keys];

    /// <summary>
    /// Gets a value indicating whether an agent is known.
    /// </summary>
    /// <param name="agent">The agent's name.</param>
    /// <returns><see langword="true"/> when it is.</returns>
    public static bool IsKnown(string agent) => string.Equals(agent, Claude, StringComparison.OrdinalIgnoreCase) || _acpCommands.ContainsKey(agent);

    /// <summary>
    /// Gets the product name of an agent: what it is called on a board unless it is given another name.
    /// </summary>
    /// <param name="agent">The agent's name, as given to --agent.</param>
    /// <returns>The product name.</returns>
    public static string ProductName(string agent)
    {
        if (string.Equals(agent, Claude, StringComparison.OrdinalIgnoreCase)) return "Claude Code";
        return _acpCommands.TryGetValue(agent, out var known) ? known.Name : agent;
    }

    /// <summary>
    /// Starts a session with an agent.
    /// </summary>
    /// <param name="agent">The agent's name.</param>
    /// <param name="command">The command that starts it, overriding the one this host knows, or <see langword="null"/>.</param>
    /// <param name="session">The session.</param>
    /// <returns>The agent.</returns>
    /// <exception cref="ArgumentException">The agent is not known, or needs a command this host does not know.</exception>
    public static IBuildAgent Create(string agent, string? command, BuildAgentSession session)
    {
        if (string.Equals(agent, Claude, StringComparison.OrdinalIgnoreCase))
        {
            return new ClaudeCodeAgent(session, string.IsNullOrWhiteSpace(command) ? "claude" : command);
        }

        if (!_acpCommands.TryGetValue(agent, out var known))
        {
            throw new ArgumentException($"'{agent}' is not an agent this CLI runs. Use one of {string.Join(", ", Names)}, or --exec for a generator.");
        }

        var line = string.IsNullOrWhiteSpace(command) ? known.Command : command;
        if (string.IsNullOrWhiteSpace(line))
        {
            throw new ArgumentException($"{known.Name} needs --agent-command: the command that starts its Agent Client Protocol server.");
        }

        var parts = Split(line);
        return new AcpAgent(session, known.Name, parts[0], parts[1..]);
    }

    /// <summary>
    /// Splits a command line into the program and its arguments, honoring double and single quotes.
    /// </summary>
    /// <param name="line">The command line.</param>
    /// <returns>The program followed by its arguments.</returns>
    /// <exception cref="ArgumentException">The command line is empty.</exception>
    public static string[] Split(string line)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        char? quote = null;
        var started = false;
        foreach (var character in line)
        {
            if (quote is { } open)
            {
                if (character == open) quote = null;
                else current.Append(character);
            }
            else if (character is '"' or '\'')
            {
                quote = character;
                started = true;
            }
            else if (char.IsWhiteSpace(character))
            {
                if (started) parts.Add(current.ToString());
                current.Clear();
                started = false;
            }
            else
            {
                current.Append(character);
                started = true;
            }
        }

        if (started) parts.Add(current.ToString());
        return parts.Count > 0 ? [.. parts] : throw new ArgumentException("The agent command is empty.");
    }
}
