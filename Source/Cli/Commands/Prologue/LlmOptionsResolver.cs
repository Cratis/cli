// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Cli.Commands.Llm;
using Cratis.Prologue.Configuration;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Resolves language-model refinement with command-line disablement first, explicit local settings next,
/// and the CLI's global configuration only when the local enabled setting is absent.
/// </summary>
public static class LlmOptionsResolver
{
    /// <summary>
    /// Resolves the language-model options for an interpretation run.
    /// </summary>
    /// <param name="prologueConfiguration">Explicit local options; <see langword="null"/> when not configured.</param>
    /// <param name="cliConfiguration">The global model configuration; <see langword="null"/> when not configured.</param>
    /// <returns>The local options, including explicit disablement, or the global fallback.</returns>
    public static LlmOptions Resolve(PrologueConfiguration? prologueConfiguration, LlmConfiguration? cliConfiguration)
    {
        if (prologueConfiguration is not null)
        {
            return prologueConfiguration.Llm;
        }

        return KindFor(cliConfiguration) is { } kind ? FromCli(cliConfiguration!, kind) : new LlmOptions { Enabled = false };
    }

    /// <summary>
    /// Resolves refinement and its source while preserving the distinction between absent and false local settings.
    /// </summary>
    /// <param name="prologueConfigurationJson">The local configuration JSON; <see langword="null"/> when none exists.</param>
    /// <param name="cliConfiguration">The CLI's language model configuration; <see langword="null"/> when not configured.</param>
    /// <param name="noLlm">Whether the command line forces heuristics-only interpretation.</param>
    /// <returns>The resolved options and their source; disabled when requested or nothing is configured.</returns>
    public static (LlmOptions Options, string Source) ResolveWithSource([StringSyntax(StringSyntaxAttribute.Json)] string? prologueConfigurationJson, LlmConfiguration? cliConfiguration, bool noLlm = false)
    {
        if (noLlm)
        {
            return (new LlmOptions { Enabled = false }, "--no-llm");
        }

        // The package's non-nullable Enabled property cannot distinguish an absent setting from false.
        // Use the same serializer options so property casing and duplicate-property precedence agree.
        var localEnabled = prologueConfigurationJson is not null
            ? JsonSerializer.Deserialize<LocalConfiguration>(prologueConfigurationJson, PrologueConfigurationFile.SerializerOptions)?.Llm?.Enabled
            : null;
        if (localEnabled is { } enabled)
        {
            return (enabled ? PrologueConfigurationFile.Read(prologueConfigurationJson!).Llm : new LlmOptions { Enabled = false }, "local file");
        }

        if (KindFor(cliConfiguration) is { } kind)
        {
            return (FromCli(cliConfiguration!, kind), "global config");
        }

        return (new LlmOptions { Enabled = false }, "none");
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

    sealed record LocalConfiguration(LocalLlm? Llm);

    sealed record LocalLlm(bool? Enabled);
}
