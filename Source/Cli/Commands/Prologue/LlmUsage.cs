// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Prologue.Configuration;
using Cratis.Prologue.Interpretation;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Describes the effective language model without exposing credentials or endpoint paths.
/// </summary>
/// <param name="Used">Whether language-model refinement is enabled for this run.</param>
/// <param name="Kind">The provider kind, or none.</param>
/// <param name="Model">The effective model identifier, or an empty string.</param>
/// <param name="EndpointHost">The effective endpoint host, or an empty string.</param>
/// <param name="Source">Where the refinement setting came from.</param>
public record LlmUsage(bool Used, string Kind, string Model, string EndpointHost, string Source)
{
    /// <summary>
    /// Gets the notice shown before any capture evidence can be sent.
    /// </summary>
    [JsonIgnore]
    public string Notice => Used
        ? $"Language model: {Kind}; model: {Model}; endpoint host: {EndpointHost}; source: {Source}. Capture evidence will be sent to this provider."
        : $"Language model: none; source: {Source}. Interpreting with heuristics only.";

    /// <summary>
    /// Describes the provider selected for interpretation, including the chat client's defaults.
    /// </summary>
    /// <param name="options">The resolved language-model options.</param>
    /// <param name="source">The source of the options.</param>
    /// <returns>The credential-free provider description.</returns>
    public static LlmUsage From(LlmOptions options, string source)
    {
        if (!options.Enabled)
        {
            return new(false, "none", string.Empty, string.Empty, source);
        }

        // OpenAI always uses its public API. Anthropic treats the default Ollama endpoint as unset.
        var endpoint = options.Kind switch
        {
            LlmKind.OpenAI => "https://api.openai.com",
            LlmKind.Anthropic when string.IsNullOrEmpty(options.Endpoint) || string.Equals(options.Endpoint, "http://llm:11434", StringComparison.OrdinalIgnoreCase) => "https://api.anthropic.com",
            _ => options.Endpoint
        };

        return new(true, options.Kind.ToString(), LlmChatClient.EffectiveModelId(options), new Uri(endpoint).Host, source);
    }
}
