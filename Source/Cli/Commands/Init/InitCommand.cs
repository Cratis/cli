// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Init;

/// <summary>
/// Generates CHRONICLE.md and configures AI tools for the current project directory.
/// </summary>
[CliCommand("init", "Generate CHRONICLE.md and configure AI tools for the current project")]
[CliExample("init")]
[CliExample("init", "--tool", "claude")]
[CliExample("init", "--force", "--no-commands")]
[LlmOption("--force", "bool", "Overwrite existing files")]
[LlmOption("--tool", "string", "Target a specific AI tool: claude, copilot, cursor, windsurf. Omit to auto-detect.")]
[LlmOption("--no-commands", "bool", "Skip generating slash commands / prompt files")]
public class InitCommand : AsyncCommand<InitSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, InitSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var basePath = Directory.GetCurrentDirectory();
        var chronicleMdPath = Path.Combine(basePath, "CHRONICLE.md");
        var allActions = new List<string>();

        // Step 1: Generate CHRONICLE.md
        if (File.Exists(chronicleMdPath) && !settings.Force)
        {
            allActions.Add("CHRONICLE.md already exists (skipped, use --force to overwrite)");
        }
        else
        {
            var existed = File.Exists(chronicleMdPath);
            var content = ChronicleDocGenerator.Generate();
            await File.WriteAllTextAsync(chronicleMdPath, content, cancellationToken);
            allActions.Add(existed ? "Overwrote CHRONICLE.md" : "Created CHRONICLE.md");
        }

        // Step 2: Determine which AI tools to configure
        IReadOnlyList<AiTool> tools;

        if (settings.Tool is not null)
        {
            if (!AiToolDetector.TryParse(settings.Tool, out var tool))
            {
                OutputFormatter.WriteError(format, $"Unknown AI tool: '{settings.Tool}'", "Valid tools: claude, copilot, cursor, windsurf", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            tools = [tool];
        }
        else
        {
            tools = AiToolDetector.Detect(basePath);
        }

        // Step 3: Configure each tool
        if (tools.Count == 0)
        {
            allActions.Add("No AI tools detected. Use --tool to target a specific tool.");
        }
        else
        {
            var includeCommands = !settings.NoCommands;
            foreach (var tool in tools)
            {
                var actions = AiToolConfigurator.Configure(tool, basePath, settings.Force, includeCommands);
                allActions.AddRange(actions);
            }
        }

        // Step 4: Output summary
        if (format is OutputFormats.Json or OutputFormats.JsonCompact)
        {
            OutputFormatter.WriteObject(format, new
            {
                chronicleMd = chronicleMdPath,
                toolsConfigured = tools.Select(t => t.ToString().ToLowerInvariant()).ToArray(),
                actions = allActions.ToArray(),
            });
        }
        else if (format is not OutputFormats.Quiet)
        {
            foreach (var action in allActions)
            {
                OutputFormatter.WriteMessage(format, action);
            }

            if (tools.Count > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]Run 'cratis llm-context' for the full machine-readable capability descriptor.[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]Run 'cratis completions install' to enable shell tab-completion.[/]");
        }

        return ExitCodes.Success;
    }
}
