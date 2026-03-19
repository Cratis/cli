// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle;

namespace Cratis.Cli.Commands.Chat;

/// <summary>
/// Settings for the chat command.
/// </summary>
public class ChatSettings : ChronicleSettings
{
    /// <summary>
    /// Gets or sets an optional single question. When provided, the chat runs non-interactively.
    /// </summary>
    [CommandArgument(0, "[QUESTION]")]
    [Description("Ask a single question and exit (non-interactive mode)")]
    public string? Question { get; set; }

    /// <summary>
    /// Gets or sets the AI provider override.
    /// </summary>
    [CommandOption("--provider <NAME>")]
    [Description("AI provider: openai, anthropic, ollama, azure-openai")]
    public string? Provider { get; set; }

    /// <summary>
    /// Gets or sets the AI model override.
    /// </summary>
    [CommandOption("--model <NAME>")]
    [Description("AI model name (e.g. gpt-4o, claude-sonnet-4-20250514, llama3.1)")]
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets whether tool calling is disabled.
    /// </summary>
    [CommandOption("--no-tools")]
    [DefaultValue(false)]
    [Description("Disable Chronicle tool calling (plain chat only)")]
    public bool NoTools { get; set; }

    /// <summary>
    /// Gets or sets an additional system prompt.
    /// </summary>
    [CommandOption("--system <PROMPT>")]
    [Description("Additional system prompt to guide the AI")]
    public string? SystemPrompt { get; set; }
}
