// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Anthropic.Core;
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
    /// Resolves and validates the endpoint, pins it in the client options, and describes the selected provider.
    /// </summary>
    /// <param name="options">The resolved language-model options.</param>
    /// <param name="source">The source of the options.</param>
    /// <returns>The credential-free provider description.</returns>
    /// <exception cref="InvalidLlmEndpoint">The endpoint is not an absolute HTTP or HTTPS URL with a host.</exception>
    public static LlmUsage From(LlmOptions options, string source)
    {
        if (!options.Enabled)
        {
            return new(false, "none", string.Empty, string.Empty, source);
        }

        // The pinned SDK's OpenAI client has no environment-based endpoint override. Other providers
        // receive an explicit endpoint. Anthropic alone reads ANTHROPIC_BASE_URL when its endpoint is unset.
        var endpoint = options.Kind switch
        {
            LlmKind.OpenAI => "https://api.openai.com/v1",
            LlmKind.Anthropic when string.IsNullOrEmpty(options.Endpoint) || string.Equals(options.Endpoint, "http://llm:11434", StringComparison.OrdinalIgnoreCase) =>
                Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL") ?? EnvironmentUrl.Production,
            _ => options.Endpoint
        };
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var destination) ||
            (destination.Scheme != Uri.UriSchemeHttp && destination.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(destination.Host))
        {
            throw new InvalidLlmEndpoint();
        }

        // AbsoluteUri also adds a trailing slash to the default Ollama URL if an Anthropic environment
        // override chooses it, so CreateAnthropic treats it as explicit rather than rereading the environment.
        options.Endpoint = destination.AbsoluteUri;

        return new(true, options.Kind.ToString(), LlmChatClient.EffectiveModelId(options), destination.Host, source);
    }
}
