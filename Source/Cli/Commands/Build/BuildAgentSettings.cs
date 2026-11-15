// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The options every command that builds shares: which agent or generator, which kit, in which project, and what to verify.
/// </summary>
public class BuildAgentSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the coding agent.
    /// </summary>
    [CommandOption("--agent <AGENT>")]
    [Description("The coding agent: claude (Claude Code, run natively), codex, gemini, copilot, opencode or cursor (over the Agent Client Protocol), or acp with --agent-command. Defaults to the agent 'cratis build init' recorded.")]
    public string? Agent { get; set; }

    /// <summary>
    /// Gets or sets the command that starts the agent.
    /// </summary>
    [CommandOption("--agent-command <COMMAND>")]
    [Description("The command that starts the agent, overriding the one this CLI knows; for an ACP agent, the command that starts its ACP server.")]
    public string? AgentCommand { get; set; }

    /// <summary>
    /// Gets or sets the generator command.
    /// </summary>
    [CommandOption("--exec <COMMAND>")]
    [Description("Build with a deterministic generator instead of an agent. The order is in the file named by STUDIO_BUILD_ORDER; the command writes its outcome to the file named by STUDIO_BUILD_OUTCOME.")]
    public string? Exec { get; set; }

    /// <summary>
    /// Gets or sets the level of the packaged Cratis builder.
    /// </summary>
    [CommandOption("--level <LEVEL>")]
    [Description("Build with the packaged Cratis builder instead of an agent of your own, at one of Studio's implementation modes: screenplay (Screenplay only: commit the model, no code), code (Static code: render the model into code), code-ai (Static code refined by AI: render, then an agent implements the slice descriptions, described handlers, specifications and feedback on top; needs --agent) or ai (AI only: an agent implements the slices from the model with no render; needs --agent).")]
    public string? Level { get; set; }

    /// <summary>
    /// Gets or sets the render target of the packaged builder.
    /// </summary>
    [CommandOption("--target <TARGET>")]
    [Description("With --level, what the application is built as: cratis (the default; .NET on Arc and Chronicle) or axon (Java on Axon Framework). The renderer's target at code and code-ai, and the stack the agent writes at ai.")]
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets where the agent's model calls are made.
    /// </summary>
    [CommandOption("--ai <ROUTE>")]
    [Description("Where the agent's model calls go: local (the default; the agent's own sign-in and provider) or studio (through Studio's AI gateway, on the organization's providers, so usage is counted and pooled there). Studio needs protocol version 2.")]
    public string? Ai { get; set; }

    /// <summary>
    /// Gets or sets the kit.
    /// </summary>
    [CommandOption("--kit <PATH>")]
    [Description("The build kit folder. Defaults to the kit 'cratis build init' installed in the project.")]
    public string? Kit { get; set; }

    /// <summary>
    /// Gets or sets the project folder.
    /// </summary>
    [CommandOption("--project <PATH>")]
    [Description("The project the slices are built in. Defaults to the current folder.")]
    public string? Project { get; set; }

    /// <summary>
    /// Gets or sets the verification commands besides the kit's.
    /// </summary>
    [CommandOption("--verify <COMMAND>")]
    [Description("A verification command to run after the agent, besides the kit's. Repeat for more. A slice is reported built only when every one exits with 0.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Spectre.Console.Cli uses arrays for repeatable options.")]
    public string[] Verify { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the Screenplay guidance is left out of the session.
    /// </summary>
    [CommandOption("--no-guidance")]
    [Description("Do not fetch the Screenplay session guidance for the agent.")]
    public bool NoGuidance { get; set; }

    /// <summary>
    /// Gets the project folder, resolved.
    /// </summary>
    internal bool UsesStudioAi => string.Equals(Ai, "studio", StringComparison.OrdinalIgnoreCase);

    internal BuildLevel? ResolvedLevel => BuildLevels.TryParse(Level, out var level) ? level : null;

    internal string ResolvedTarget => (Target ?? BuildLevels.CratisTarget).ToLowerInvariant();

    internal string ProjectPath => Path.GetFullPath(Project ?? Directory.GetCurrentDirectory());

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (Level is not null && !BuildLevels.TryParse(Level, out _)) return ValidationResult.Error($"'{Level}' is not a level. Use one of {string.Join(", ", BuildLevels.Names)}.");
        if (Level is not null && Exec is not null) return ValidationResult.Error("Use either --level or --exec, not both.");
        if (Target is not null && Level is null) return ValidationResult.Error("--target goes with --level.");
        if (Target is not null && !BuildLevels.IsKnownTarget(Target)) return ValidationResult.Error($"'{Target}' is not a render target. Use cratis or axon.");
        if (Ai is not null && !string.Equals(Ai, "local", StringComparison.OrdinalIgnoreCase) && !string.Equals(Ai, "studio", StringComparison.OrdinalIgnoreCase)) return ValidationResult.Error("--ai takes local or studio.");
        if (Agent is not null && Exec is not null) return ValidationResult.Error("Use either --agent or --exec, not both.");
        if (Agent is not null && !BuildAgents.IsKnown(Agent)) return ValidationResult.Error($"'{Agent}' is not an agent this CLI runs. Use one of {string.Join(", ", BuildAgents.Names)}.");
        return base.Validate();
    }
}
