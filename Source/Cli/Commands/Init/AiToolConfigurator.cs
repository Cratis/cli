// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Init;

/// <summary>
/// Configures AI tool integrations to reference CHRONICLE.md.
/// </summary>
public static class AiToolConfigurator
{
    const string ChronicleReference = "@CHRONICLE.md";
    const string DiagnoseCommandName = "chronicle-diagnose";

    /// <summary>
    /// Configures the specified AI tool to reference CHRONICLE.md.
    /// </summary>
    /// <param name="tool">The AI tool to configure.</param>
    /// <param name="basePath">The project base directory.</param>
    /// <param name="configuration">What to write for this tool.</param>
    /// <returns>A list of actions taken.</returns>
    public static IReadOnlyList<string> Configure(AiTool tool, string basePath, AiToolConfiguration configuration)
    {
        return tool switch
        {
            AiTool.Claude => ConfigureClaude(basePath, configuration),
            AiTool.Copilot => ConfigureCopilot(basePath, configuration),
            AiTool.Cursor => ConfigureCursor(basePath, configuration),
            AiTool.Windsurf => ConfigureWindsurf(basePath, configuration),
            AiTool.Pi => ConfigurePi(basePath, configuration),
            _ => [],
        };
    }

    /// <summary>
    /// Regenerates any skill/command files that were previously created by <c>cratis init</c>.
    /// Only updates files that already exist — does not create new ones.
    /// </summary>
    /// <param name="basePath">The project base directory.</param>
    /// <param name="llmContextJson">The serialized llm-context JSON to embed in skill files.</param>
    /// <returns>A list of actions taken.</returns>
    public static IReadOnlyList<string> RefreshSkillFiles(string basePath, string llmContextJson)
    {
        var actions = new List<string>();
        var skillContent = ChronicleSkillGenerator.Generate(llmContextJson);

        var copilotSkillPath = Path.Combine(basePath, ".github", "skills", ChronicleSkillGenerator.SkillName, "SKILL.md");
        if (File.Exists(copilotSkillPath))
        {
            File.WriteAllText(copilotSkillPath, skillContent);
            actions.Add($"Refreshed .github/skills/{ChronicleSkillGenerator.SkillName}/SKILL.md");
        }

        var claudeCommandPath = Path.Combine(basePath, ".claude", "commands", $"{ChronicleSkillGenerator.SkillName}.md");
        if (File.Exists(claudeCommandPath))
        {
            File.WriteAllText(claudeCommandPath, skillContent);
            actions.Add($"Refreshed .claude/commands/{ChronicleSkillGenerator.SkillName}.md");
        }

        var piSkillPath = Path.Combine(basePath, ".pi", "skills", ChronicleSkillGenerator.SkillName, "SKILL.md");
        if (File.Exists(piSkillPath))
        {
            File.WriteAllText(piSkillPath, skillContent);
            actions.Add($"Refreshed .pi/skills/{ChronicleSkillGenerator.SkillName}/SKILL.md");
        }

        return actions;
    }

    /// <summary>
    /// Reports a context reference that was deliberately not written, and says what to do instead.
    /// </summary>
    /// <remarks>
    /// Skipping silently would leave a project that looks configured and loads nothing - the skill is on
    /// disk but no instruction file points at <c>CHRONICLE.md</c>, so an agent never reads it. Naming the
    /// file and the line to add turns the skip into an instruction rather than an omission.
    /// </remarks>
    /// <param name="file">The instruction file that was left alone.</param>
    /// <returns>The action to report.</returns>
    static string SkippedContext(string file) =>
        $"Skipped the @CHRONICLE.md reference in {file} (--no-context) - add it to whatever generates that file, or the catalog is written but never loaded";

    static List<string> ConfigureClaude(string basePath, AiToolConfiguration configuration)
    {
        var actions = new List<string>();
        var claudeMd = Path.Combine(basePath, "CLAUDE.md");

        if (!configuration.IncludeContext)
        {
            actions.Add(SkippedContext("CLAUDE.md"));
        }
        else if (File.Exists(claudeMd))
        {
            var content = File.ReadAllText(claudeMd);
            if (!content.Contains(ChronicleReference, StringComparison.Ordinal))
            {
                File.AppendAllText(claudeMd, $"\n{ChronicleReference}\n");
                actions.Add("Appended @CHRONICLE.md reference to CLAUDE.md");
            }
            else
            {
                actions.Add("CLAUDE.md already references @CHRONICLE.md (skipped)");
            }
        }
        else
        {
            File.WriteAllText(claudeMd, $"{ChronicleReference}\n");
            actions.Add("Created CLAUDE.md with @CHRONICLE.md reference");
        }

        if (configuration.IncludeCommands)
        {
            var commandsDir = Path.Combine(basePath, ".claude", "commands");
            var commandPath = Path.Combine(commandsDir, $"{DiagnoseCommandName}.md");

            if (!File.Exists(commandPath) || configuration.Force)
            {
                Directory.CreateDirectory(commandsDir);
                File.WriteAllText(commandPath, SlashCommands.ChronicleDiagnose);
                actions.Add($"Created .claude/commands/{DiagnoseCommandName}.md");
            }
            else
            {
                actions.Add($".claude/commands/{DiagnoseCommandName}.md already exists (skipped, use --force to overwrite)");
            }

            var skillPath = Path.Combine(commandsDir, $"{ChronicleSkillGenerator.SkillName}.md");

            if (!File.Exists(skillPath) || configuration.Force)
            {
                Directory.CreateDirectory(commandsDir);
                File.WriteAllText(skillPath, ChronicleSkillGenerator.Generate(configuration.LlmContextJson));
                actions.Add($"Created .claude/commands/{ChronicleSkillGenerator.SkillName}.md");
            }
            else
            {
                actions.Add($".claude/commands/{ChronicleSkillGenerator.SkillName}.md already exists (skipped, use --force to overwrite)");
            }
        }

        return actions;
    }

    static List<string> ConfigureCopilot(string basePath, AiToolConfiguration configuration)
    {
        var actions = new List<string>();
        var instructionsPath = Path.Combine(basePath, ".github", "copilot-instructions.md");

        if (!configuration.IncludeContext)
        {
            actions.Add(SkippedContext(".github/copilot-instructions.md"));
        }
        else if (File.Exists(instructionsPath))
        {
            var content = File.ReadAllText(instructionsPath);
            if (!content.Contains(ChronicleReference, StringComparison.Ordinal))
            {
                File.AppendAllText(instructionsPath, $"\n{ChronicleReference}\n");
                actions.Add("Appended @CHRONICLE.md reference to .github/copilot-instructions.md");
            }
            else
            {
                actions.Add(".github/copilot-instructions.md already references @CHRONICLE.md (skipped)");
            }
        }
        else
        {
            var dir = Path.GetDirectoryName(instructionsPath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(instructionsPath, $"{ChronicleReference}\n");
            actions.Add("Created .github/copilot-instructions.md with @CHRONICLE.md reference");
        }

        if (configuration.IncludeCommands)
        {
            var promptsDir = Path.Combine(basePath, ".github", "copilot", "prompts");
            var promptPath = Path.Combine(promptsDir, $"{DiagnoseCommandName}.prompt.md");

            if (!File.Exists(promptPath) || configuration.Force)
            {
                Directory.CreateDirectory(promptsDir);
                File.WriteAllText(promptPath, SlashCommands.ChronicleDiagnose);
                actions.Add($"Created .github/copilot/prompts/{DiagnoseCommandName}.prompt.md");
            }
            else
            {
                actions.Add($".github/copilot/prompts/{DiagnoseCommandName}.prompt.md already exists (skipped, use --force to overwrite)");
            }

            var skillDir = Path.Combine(basePath, ".github", "skills", ChronicleSkillGenerator.SkillName);
            var skillPath = Path.Combine(skillDir, "SKILL.md");

            if (!File.Exists(skillPath) || configuration.Force)
            {
                Directory.CreateDirectory(skillDir);
                File.WriteAllText(skillPath, ChronicleSkillGenerator.Generate(configuration.LlmContextJson));
                actions.Add($"Created .github/skills/{ChronicleSkillGenerator.SkillName}/SKILL.md");
            }
            else
            {
                actions.Add($".github/skills/{ChronicleSkillGenerator.SkillName}/SKILL.md already exists (skipped, use --force to overwrite)");
            }
        }

        return actions;
    }

    static List<string> ConfigureCursor(string basePath, AiToolConfiguration configuration)
    {
        var actions = new List<string>();

        if (!configuration.IncludeContext)
        {
            return [SkippedContext(".cursor/rules/chronicle.mdc")];
        }

        var rulesDir = Path.Combine(basePath, ".cursor", "rules");
        var rulePath = Path.Combine(rulesDir, "chronicle.mdc");

        if (!File.Exists(rulePath) || configuration.Force)
        {
            Directory.CreateDirectory(rulesDir);
            File.WriteAllText(rulePath, $"{ChronicleReference}\n");
            actions.Add("Created .cursor/rules/chronicle.mdc with @CHRONICLE.md reference");
        }
        else
        {
            actions.Add(".cursor/rules/chronicle.mdc already exists (skipped, use --force to overwrite)");
        }

        return actions;
    }

    static List<string> ConfigureWindsurf(string basePath, AiToolConfiguration configuration)
    {
        var actions = new List<string>();

        if (!configuration.IncludeContext)
        {
            return [SkippedContext(".windsurfrules")];
        }

        var rulesPath = Path.Combine(basePath, ".windsurfrules");

        if (File.Exists(rulesPath))
        {
            var content = File.ReadAllText(rulesPath);
            if (!content.Contains(ChronicleReference, StringComparison.Ordinal))
            {
                File.AppendAllText(rulesPath, $"\n{ChronicleReference}\n");
                actions.Add("Appended @CHRONICLE.md reference to .windsurfrules");
            }
            else
            {
                actions.Add(".windsurfrules already references @CHRONICLE.md (skipped)");
            }
        }
        else if (configuration.Force)
        {
            File.WriteAllText(rulesPath, $"{ChronicleReference}\n");
            actions.Add("Created .windsurfrules with @CHRONICLE.md reference");
        }
        else
        {
            actions.Add("No .windsurfrules found (skipped — Windsurf detected but no rules file exists)");
        }

        return actions;
    }

    /// <summary>
    /// Configures Pi, whose project resources live under <c>.pi/</c>.
    /// </summary>
    /// <remarks>
    /// The context reference goes in <c>AGENTS.md</c> rather than a Pi-specific file, because that is what
    /// Pi reads and because it is the cross-tool convention - a project already carrying one for another
    /// agent gets the reference appended rather than a second file to keep in sync. Skills are discovered
    /// from <c>.pi/skills/&lt;name&gt;/SKILL.md</c>, which is the same directory-with-frontmatter shape
    /// Copilot uses, so the generated skill is written unchanged.
    /// </remarks>
    /// <param name="basePath">The project base directory.</param>
    /// <param name="configuration">What to write for Pi.</param>
    /// <returns>A list of actions taken.</returns>
    static List<string> ConfigurePi(string basePath, AiToolConfiguration configuration)
    {
        var actions = new List<string>();
        var agentsMd = Path.Combine(basePath, "AGENTS.md");

        if (!configuration.IncludeContext)
        {
            actions.Add(SkippedContext("AGENTS.md"));
        }
        else if (File.Exists(agentsMd))
        {
            var content = File.ReadAllText(agentsMd);
            if (!content.Contains(ChronicleReference, StringComparison.Ordinal))
            {
                File.AppendAllText(agentsMd, $"\n{ChronicleReference}\n");
                actions.Add("Appended @CHRONICLE.md reference to AGENTS.md");
            }
            else
            {
                actions.Add("AGENTS.md already references @CHRONICLE.md (skipped)");
            }
        }
        else
        {
            File.WriteAllText(agentsMd, $"{ChronicleReference}\n");
            actions.Add("Created AGENTS.md with @CHRONICLE.md reference");
        }

        if (configuration.IncludeCommands)
        {
            var promptsDir = Path.Combine(basePath, ".pi", "prompts");
            var promptPath = Path.Combine(promptsDir, $"{DiagnoseCommandName}.md");

            if (!File.Exists(promptPath) || configuration.Force)
            {
                Directory.CreateDirectory(promptsDir);
                File.WriteAllText(promptPath, SlashCommands.ChronicleDiagnose);
                actions.Add($"Created .pi/prompts/{DiagnoseCommandName}.md");
            }
            else
            {
                actions.Add($".pi/prompts/{DiagnoseCommandName}.md already exists (skipped, use --force to overwrite)");
            }

            var skillDir = Path.Combine(basePath, ".pi", "skills", ChronicleSkillGenerator.SkillName);
            var skillPath = Path.Combine(skillDir, "SKILL.md");

            if (!File.Exists(skillPath) || configuration.Force)
            {
                Directory.CreateDirectory(skillDir);
                File.WriteAllText(skillPath, ChronicleSkillGenerator.Generate(configuration.LlmContextJson));
                actions.Add($"Created .pi/skills/{ChronicleSkillGenerator.SkillName}/SKILL.md");
            }
            else
            {
                actions.Add($".pi/skills/{ChronicleSkillGenerator.SkillName}/SKILL.md already exists (skipped, use --force to overwrite)");
            }
        }

        return actions;
    }
}
