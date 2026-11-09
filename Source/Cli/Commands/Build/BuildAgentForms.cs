// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Where each coding agent reads a kit's instructions and skills in a project: Claude Code reads
/// <c language="csharp">CLAUDE.md</c> and <c language="csharp">.claude/skills</c>, Gemini CLI <c language="csharp">GEMINI.md</c>,
/// and the others <c language="csharp">AGENTS.md</c>; every agent but Claude Code reads skills from
/// <c language="csharp">.agents/skills</c>.
/// </summary>
/// <param name="Instructions">The instruction file, relative to the project.</param>
/// <param name="Skills">The skills folder, relative to the project.</param>
public sealed record BuildAgentForm(string Instructions, string Skills)
{
    /// <summary>
    /// Gets the form an agent reads.
    /// </summary>
    /// <param name="agent">The agent, or <see langword="null"/> for the form most agents read.</param>
    /// <returns>The form.</returns>
    public static BuildAgentForm For(string? agent) => agent?.ToLowerInvariant() switch
    {
        BuildAgents.Claude => new("CLAUDE.md", ".claude/skills"),
        "gemini" => new("GEMINI.md", ".agents/skills"),
        _ => new("AGENTS.md", ".agents/skills")
    };
}
