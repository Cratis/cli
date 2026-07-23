// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Llm;
using Cratis.Prologue.Configuration;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Resolves the <see cref="LlmOptions"/> the interpreter refines with — the <c>Llm</c> section of a
/// <c>cratis-prologue.json</c> when it is enabled there, otherwise the <c>llm</c> section of the CLI's own
/// configuration (written by <c>cratis llm use</c>), and disabled refinement when neither is configured.
/// </summary>
public static class LlmOptionsResolver
{
    /// <summary>
    /// Resolves the language-model options for an interpretation run.
    /// </summary>
    /// <param name="prologueConfiguration">The <c>cratis-prologue.json</c> configuration found for the run; <see langword="null"/> when none exists.</param>
    /// <param name="cliConfiguration">The CLI's language model configuration; <see langword="null"/> when not configured.</param>
    /// <returns>The resolved <see cref="LlmOptions"/>; disabled when nothing is configured.</returns>
    public static LlmOptions Resolve(PrologueConfiguration? prologueConfiguration, LlmConfiguration? cliConfiguration)
    {
        if (prologueConfiguration?.Llm.Enabled == true)
        {
            return prologueConfiguration.Llm;
        }

        if (KindFor(cliConfiguration) is { } kind)
        {
            return FromCli(cliConfiguration!, kind);
        }

        return new LlmOptions { Enabled = false };
    }

    static LlmKind? KindFor(LlmConfiguration? configuration) =>
        configuration?.Kind is { Length: > 0 } value && LlmKinds.IsValid(value)
            ? LlmKinds.Normalize(value) switch
            {
                LlmKinds.Anthropic => LlmKind.Anthropic,
                LlmKinds.OpenAI => LlmKind.OpenAI,
                LlmKinds.Local => LlmKind.OpenAICompatible,
                _ => null
            }
            : null;

    static LlmOptions FromCli(LlmConfiguration configuration, LlmKind kind)
    {
        // An empty model falls back to the provider's default model, and only an explicitly configured
        // endpoint overrides the LlmOptions default — the chat client treats that default as "use the hosted
        // provider's public endpoint".
        var options = new LlmOptions
        {
            Enabled = true,
            Kind = kind,
            AccessToken = configuration.ApiKey ?? string.Empty,
            ModelId = configuration.Model ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(configuration.Endpoint))
        {
            options.Endpoint = configuration.Endpoint;
        }

        return options;
    }
}
