// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Composes how Claude Code is started for a session: the guidance as a plugin and a system-prompt addition, the kit's
/// skills as a plugin, the MCP servers in a configuration that reads every credential from the environment, and the
/// permissions as allowed tools with every other request refused. Permission checks are never switched off.
/// </summary>
public static class ClaudeCodeArguments
{
    /// <summary>The environment variable prefix credentials of MCP servers are passed in.</summary>
    public const string TokenVariablePrefix = "CRATIS_BUILD_MCP_TOKEN_";

    /// <summary>
    /// Gets the environment variable the bearer token of an MCP server is passed in.
    /// </summary>
    /// <param name="tool">The server.</param>
    /// <returns>The variable's name.</returns>
    public static string TokenVariable(BuildAgentTool tool) => TokenVariablePrefix + new string([.. tool.Name.ToUpperInvariant().Select(character => char.IsAsciiLetterOrDigit(character) ? character : '_')]);

    /// <summary>
    /// Writes the MCP configuration of a session. A remote server's token is referenced as an environment variable, never
    /// written into the file.
    /// </summary>
    /// <param name="tools">The servers.</param>
    /// <returns>The configuration document.</returns>
    public static JsonObject McpConfiguration(IEnumerable<BuildAgentTool> tools)
    {
        var servers = new JsonObject();
        foreach (var tool in tools)
        {
            servers[tool.Name] = tool.Command is not null
                ? new JsonObject { ["command"] = tool.Command, ["args"] = new JsonArray([.. tool.Arguments.Select(argument => (JsonNode)argument)]) }
                : new JsonObject
                {
                    ["type"] = "http",
                    ["url"] = tool.Url,
                    ["headers"] = Headers(tool)
                };
        }

        return new JsonObject { ["mcpServers"] = servers };
    }

    /// <summary>
    /// Lists the tools Claude Code may use without asking; it is started so that it refuses everything else.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <returns>The allowed tool rules.</returns>
    public static IReadOnlyList<string> AllowedTools(BuildAgentSession session)
    {
        var permissions = session.Permissions;
        var project = Absolute(permissions.Project);
        var allowed = new List<string> { "Read", "Glob", "Grep", "LS", "TodoWrite", "Task" };
        foreach (var glob in permissions.WritablePaths)
        {
            var normalized = glob.StartsWith("./", StringComparison.Ordinal) ? glob[2..] : glob;
            allowed.Add($"Edit({project}/{normalized})");
            allowed.Add($"Write({project}/{normalized})");
        }

        foreach (var file in permissions.WritableFiles)
        {
            allowed.Add($"Edit({Absolute(file)})");
            allowed.Add($"Write({Absolute(file)})");
        }

        allowed.AddRange(permissions.Commands.Where(command => command.Trim().Length > 0).Select(command => $"Bash({command.Trim()}:*)"));
        foreach (var tool in session.Tools)
        {
            if (session.MayChangeTools || tool.ReadOnlyTools.Count == 0)
            {
                allowed.Add($"mcp__{tool.Name}");
            }
            else
            {
                allowed.AddRange(tool.ReadOnlyTools.Select(name => $"mcp__{tool.Name}__{name}"));
            }
        }

        return allowed;
    }

    /// <summary>
    /// Composes the arguments of one prompt.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="systemPrompt">The file holding the system-prompt addition.</param>
    /// <param name="mcpConfiguration">The file holding the MCP configuration.</param>
    /// <param name="kitPlugin">The folder holding the kit's skills as a plugin, or <see langword="null"/>.</param>
    /// <param name="resume">The session to continue, or <see langword="null"/> for a new one.</param>
    /// <returns>The arguments; the prompt itself goes to the standard input.</returns>
    public static IReadOnlyList<string> For(BuildAgentSession session, string systemPrompt, string mcpConfiguration, string? kitPlugin, string? resume)
    {
        var arguments = new List<string>
        {
            "-p", "--output-format", "json",
            "--permission-mode", "dontAsk",
            "--append-system-prompt-file", systemPrompt,
            "--mcp-config", mcpConfiguration, "--strict-mcp-config",
            "--add-dir", session.Workspace
        };

        if (session.Guidance is { IsClaudePlugin: true } guidance) arguments.AddRange(["--plugin-dir", guidance.Root]);
        if (kitPlugin is not null) arguments.AddRange(["--plugin-dir", kitPlugin]);
        if (resume is not null) arguments.AddRange(["--resume", resume]);
        arguments.Add("--allowedTools");
        arguments.AddRange(AllowedTools(session));
        return arguments;
    }

    static JsonObject Headers(BuildAgentTool tool)
    {
        var headers = new JsonObject();
        if (tool.BearerToken is not null) headers["Authorization"] = $"Bearer ${{{TokenVariable(tool)}}}";
        foreach (var (name, value) in tool.Headers) headers[name] = value;
        return headers;
    }

    /// <summary>
    /// Writes a path the way Claude Code reads it as absolute in a permission rule: starting with <c language="csharp">//</c>.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The rule path.</returns>
    static string Absolute(string path) => "/" + Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
}
