// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Claude Code, run natively through its own command line: nothing else to install. Each prompt runs Claude Code once;
/// later prompts of the same session continue the conversation.
/// </summary>
/// <param name="session">The session.</param>
/// <param name="program">The Claude Code command.</param>
public sealed class ClaudeCodeAgent(BuildAgentSession session, string program = "claude") : IBuildAgent
{
    string? _conversation;

    /// <inheritdoc/>
    public string Name => "Claude Code";

    /// <inheritdoc/>
    public async Task<BuildAgentReply> Prompt(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
    {
        var files = Path.Combine(session.Workspace, "claude");
        Directory.CreateDirectory(files);
        var systemPrompt = Path.Combine(files, "system-prompt.md");
        await File.WriteAllTextAsync(systemPrompt, SystemPrompt(), cancellationToken);
        var mcp = Path.Combine(files, "mcp.json");
        await File.WriteAllTextAsync(mcp, ClaudeCodeArguments.McpConfiguration(session.Tools).ToJsonString(), cancellationToken);
        var kitPlugin = KitPlugin(files);

        var variables = new Dictionary<string, string>(environment, StringComparer.Ordinal);
        foreach (var tool in session.Tools.Where(tool => tool.BearerToken is not null))
        {
            variables[ClaudeCodeArguments.TokenVariable(tool)] = tool.BearerToken!;
        }

        session.Log($"{Name} is working.");
        var result = await BuildProcess.Run(
            program,
            ClaudeCodeArguments.For(session, systemPrompt, mcp, kitPlugin, _conversation),
            session.Project,
            variables,
            prompt,
            null,
            cancellationToken);
        return Read(result);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Reads what Claude Code answered with in its JSON output.
    /// </summary>
    /// <param name="result">The process result.</param>
    /// <returns>The reply.</returns>
    internal BuildAgentReply Read(BuildProcessResult result)
    {
        var json = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).LastOrDefault(line => line.TrimStart().StartsWith('{'));
        JsonObject? answer = null;
        try
        {
            answer = json is null ? null : JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
        }

        if (answer is null)
        {
            var reason = result.ExitCode == 127 ? result.Error : $"{Name} exited with {result.ExitCode} without an answer.";
            return BuildAgentReply.Failure(reason.Trim());
        }

        _conversation = answer["session_id"]?.GetValue<string>() ?? _conversation;
        var reply = answer["result"]?.ToString() ?? string.Empty;
        var failed = result.ExitCode != 0 || (answer["is_error"] is JsonValue isError && isError.TryGetValue<bool>(out var error) && error);
        var usage = answer["usage"];
        var failure = reply.Length > 0 ? reply : $"{Name} exited with {result.ExitCode}.";
        return new(
            reply,
            !failed,
            failed ? failure : null,
            Count(usage?["input_tokens"]) + Count(usage?["cache_read_input_tokens"]) + Count(usage?["cache_creation_input_tokens"]),
            Count(usage?["output_tokens"]));
    }

    static long Count(JsonNode? node) => node is JsonValue value && value.TryGetValue<long>(out var count) ? count : 0;

    string SystemPrompt()
    {
        var parts = new List<string>();
        if (session.Guidance?.SystemPrompt is { } guidance) parts.Add(File.ReadAllText(guidance));
        if (session.Instructions.Length > 0) parts.Add(session.Instructions);
        return string.Join($"{Environment.NewLine}{Environment.NewLine}", parts);
    }

    /// <summary>
    /// Makes the kit's skills folder a plugin for this session only, since Claude Code loads skills from plugins.
    /// </summary>
    /// <param name="files">The session's own folder.</param>
    /// <returns>The plugin folder, or <see langword="null"/> when the kit has no skills.</returns>
    string? KitPlugin(string files)
    {
        if (session.Skills is null || !Directory.Exists(session.Skills)) return null;
        var plugin = Path.Combine(files, "build-kit");
        if (Directory.Exists(plugin)) Directory.Delete(plugin, recursive: true);
        Directory.CreateDirectory(Path.Combine(plugin, ".claude-plugin"));
        File.WriteAllText(Path.Combine(plugin, ".claude-plugin", "plugin.json"), new JsonObject { ["name"] = "build-kit", ["version"] = "1.0.0", ["description"] = "The build kit's skills." }.ToJsonString());
        BuildFiles.CopyFolder(session.Skills, Path.Combine(plugin, "skills"));
        return plugin;
    }
}
