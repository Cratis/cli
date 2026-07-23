// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Llm;

/// <summary>
/// Well-known language model provider kinds and their defaults.
/// </summary>
public static class LlmKinds
{
    /// <summary>
    /// The Anthropic provider kind.
    /// </summary>
    public const string Anthropic = "anthropic";

    /// <summary>
    /// The OpenAI provider kind.
    /// </summary>
    public const string OpenAI = "openai";

    /// <summary>
    /// The local (OpenAI-compatible endpoint) provider kind.
    /// </summary>
    public const string Local = "local";

    /// <summary>
    /// The default endpoint suggested for local (OpenAI-compatible) providers.
    /// </summary>
    public const string DefaultLocalEndpoint = "http://localhost:11434/v1";

    /// <summary>
    /// All valid provider kinds.
    /// </summary>
    public static readonly string[] All = [Anthropic, OpenAI, Local];

    /// <summary>
    /// Determines whether the given kind is a known provider kind, ignoring casing.
    /// </summary>
    /// <param name="kind">The kind to validate.</param>
    /// <returns>True when the kind is known; false otherwise.</returns>
    public static bool IsValid(string? kind) =>
        kind is not null && All.Contains(kind, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Normalizes a kind to its canonical lowercase form.
    /// </summary>
    /// <param name="kind">The kind to normalize.</param>
    /// <returns>The normalized kind.</returns>
    public static string Normalize(string kind) => kind.ToLowerInvariant();

    /// <summary>
    /// Gets the default model hint for the given kind, or null when the kind has none.
    /// </summary>
    /// <param name="kind">The provider kind.</param>
    /// <returns>The default model hint, or null.</returns>
    public static string? DefaultModelFor(string kind) => Normalize(kind) switch
    {
        Anthropic => "claude-opus-4-6",
        OpenAI => "gpt-4o-mini",
        _ => null
    };
}
