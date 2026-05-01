// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Init;

/// <summary>
/// Detects which AI tools are configured in a directory or active in the runtime environment.
/// Combines file-based detection (project config files) with runtime detection (environment variables
/// set by AI tools when they spawn terminal processes).
/// </summary>
public static class AiToolDetector
{
    /// <summary>
    /// Detects AI tools present in the given directory or active in the current runtime environment.
    /// </summary>
    /// <param name="basePath">The directory to scan for project-level configuration files.</param>
    /// <returns>A list of detected AI tools (deduplicated).</returns>
    public static IReadOnlyList<AiTool> Detect(string basePath)
    {
        var tools = new HashSet<AiTool>();

        DetectFromProjectFiles(basePath, tools);
        DetectFromEnvironment(tools);

        return [.. tools];
    }

    /// <summary>
    /// Parses a tool name string into an <see cref="AiTool"/> value.
    /// </summary>
    /// <param name="name">The tool name string.</param>
    /// <param name="tool">The parsed tool, if successful.</param>
    /// <returns>True if the name was recognized; false otherwise.</returns>
    public static bool TryParse(string name, out AiTool tool)
    {
        switch (name.ToLowerInvariant())
        {
            case "claude":
                tool = AiTool.Claude;
                return true;
            case "copilot":
                tool = AiTool.Copilot;
                return true;
            case "cursor":
                tool = AiTool.Cursor;
                return true;
            case "windsurf":
                tool = AiTool.Windsurf;
                return true;
            default:
                tool = default;
                return false;
        }
    }

    /// <summary>
    /// Detects AI tools from project-level configuration files and directories.
    /// </summary>
    /// <param name="basePath">The directory to scan.</param>
    /// <param name="tools">The set to add detected tools to.</param>
    static void DetectFromProjectFiles(string basePath, HashSet<AiTool> tools)
    {
        if (Directory.Exists(Path.Combine(basePath, ".claude")) ||
            File.Exists(Path.Combine(basePath, "CLAUDE.md")))
        {
            tools.Add(AiTool.Claude);
        }

        if (File.Exists(Path.Combine(basePath, ".github", "copilot-instructions.md")))
        {
            tools.Add(AiTool.Copilot);
        }

        if (Directory.Exists(Path.Combine(basePath, ".cursor")) ||
            File.Exists(Path.Combine(basePath, ".cursorrules")))
        {
            tools.Add(AiTool.Cursor);
        }

        if (File.Exists(Path.Combine(basePath, ".windsurfrules")))
        {
            tools.Add(AiTool.Windsurf);
        }
    }

    /// <summary>
    /// Detects AI tools from environment variables set by the tool's runtime.
    /// This catches cases where the tool is active but hasn't been configured in the project yet
    /// (e.g. running <c>cratis init</c> for the first time from within Claude Code).
    /// </summary>
    /// <param name="tools">The set to add detected tools to.</param>
    static void DetectFromEnvironment(HashSet<AiTool> tools)
    {
        // Claude Code sets CLAUDECODE=1 and/or CLAUDE_CODE_ENTRYPOINT when spawning terminals.
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CLAUDECODE")) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CLAUDE_CODE_ENTRYPOINT")))
        {
            tools.Add(AiTool.Claude);
        }

        // VS Code sets VSCODE_PID and TERM_PROGRAM=vscode. Copilot is the primary AI tool in VS Code.
        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM") ?? string.Empty;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSCODE_PID")) ||
            termProgram.Equals("vscode", StringComparison.OrdinalIgnoreCase))
        {
            tools.Add(AiTool.Copilot);
        }

        // Cursor sets TERM_PROGRAM=cursor or CURSOR_TRACE_DIR when spawning terminals.
        if (termProgram.Equals("cursor", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CURSOR_TRACE_DIR")))
        {
            tools.Add(AiTool.Cursor);
        }

        // Windsurf (Codeium) sets TERM_PROGRAM=windsurf or WINDSURF_* env vars.
        if (termProgram.Equals("windsurf", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WINDSURF_SESSION_ID")))
        {
            tools.Add(AiTool.Windsurf);
        }
    }
}
