// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Llm;

/// <summary>
/// Settings for configuring the language model provider.
/// </summary>
public class UseLlmSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the provider kind to use.
    /// </summary>
    [CommandArgument(0, "<KIND>")]
    [Description("Language model provider kind: anthropic, openai, or local (OpenAI-compatible endpoint)")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key used to authenticate with the provider.
    /// </summary>
    [CommandOption("--api-key <KEY>")]
    [Description("API key for the provider. Prompted for interactively when omitted.")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URL for the provider.
    /// </summary>
    [CommandOption("--endpoint <URL>")]
    [Description("Endpoint URL for the provider. Required for local (e.g. http://localhost:11434/v1).")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the model to use with the provider.
    /// </summary>
    [CommandOption("--model <NAME>")]
    [Description("Model to use (e.g. claude-opus-4-6 for anthropic, gpt-4o-mini for openai)")]
    public string? Model { get; set; }
}
