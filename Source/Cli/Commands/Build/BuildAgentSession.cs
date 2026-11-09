// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// An MCP server the host configures on an agent session. A credential for it is never written into a prompt: the host
/// passes it in an environment variable the agent's own configuration reads, or in the session setup of the protocol.
/// </summary>
/// <param name="Name">The server's name in the session.</param>
/// <param name="Command">The command that starts a local server, or <see langword="null"/> for a remote one.</param>
/// <param name="Arguments">The arguments of the command.</param>
/// <param name="Url">The address of a remote server.</param>
/// <param name="BearerToken">The bearer token for a remote server, or <see langword="null"/>.</param>
/// <param name="ReadOnlyTools">The tools that only read, which an agent may use before a person confirmed a proposal; empty means every tool.</param>
public sealed record BuildAgentTool(
    string Name,
    string? Command,
    IReadOnlyList<string> Arguments,
    string? Url,
    string? BearerToken,
    IReadOnlyList<string> ReadOnlyTools)
{
    /// <summary>
    /// Gets the headers, other than the bearer token, that go on every request to a remote server; they carry no credential.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Everything a host gives an agent session.
/// </summary>
/// <param name="Project">The project folder the agent works in.</param>
/// <param name="Workspace">A folder outside the project, holding the order, the outcome and the session's own files.</param>
/// <param name="Instructions">The kit's stack instructions, followed by any other instructions for every prompt.</param>
/// <param name="Guidance">The Screenplay session guidance, or <see langword="null"/> when the session goes without.</param>
/// <param name="Skills">The kit's skills folder, or <see langword="null"/>.</param>
/// <param name="Tools">The MCP servers to configure on the session.</param>
/// <param name="Permissions">What the agent may do when it asks.</param>
/// <param name="Log">Where the host writes what the agent is doing, already free of credentials.</param>
public sealed record BuildAgentSession(
    string Project,
    string Workspace,
    string Instructions,
    ScreenplaySessionGuidance? Guidance,
    string? Skills,
    IReadOnlyList<BuildAgentTool> Tools,
    BuildPermissions Permissions,
    Action<string> Log)
{
    /// <summary>
    /// Gets or sets a value indicating whether the agent may use tools that change something outside the project, such as
    /// Studio's board tools. Off while a modeling proposal waits for a person.
    /// </summary>
    public bool MayChangeTools { get; set; } = true;
}

/// <summary>
/// What came of one prompt to an agent.
/// </summary>
/// <param name="Reply">The agent's final message.</param>
/// <param name="Succeeded">Whether the agent ran to the end.</param>
/// <param name="Error">Why it did not, when it did not.</param>
/// <param name="InputTokens">The input tokens it used, when the agent says.</param>
/// <param name="OutputTokens">The output tokens it used, when the agent says.</param>
public sealed record BuildAgentReply(string Reply, bool Succeeded, string? Error, long InputTokens = 0, long OutputTokens = 0)
{
    /// <summary>
    /// Creates the reply of an agent that could not run to the end.
    /// </summary>
    /// <param name="error">Why.</param>
    /// <returns>The reply.</returns>
    public static BuildAgentReply Failure(string error) => new(string.Empty, false, error);
}
