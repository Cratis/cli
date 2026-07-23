// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Represents the language model configuration used by Cratis tools like Prologue.
/// </summary>
public class LlmConfiguration
{
    /// <summary>
    /// Gets or sets the kind of language model provider (anthropic, openai, or local).
    /// </summary>
    public string? Kind { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URL for the provider. Required for local (OpenAI-compatible) providers.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the API key used to authenticate with the provider.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the model to use with the provider.
    /// </summary>
    public string? Model { get; set; }
}
