// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Registration;

/// <summary>
/// Provides an AI-focused description for a command in the LLM context descriptor.
/// When present, this description replaces the standard <c>[CliCommand]</c> description
/// in the LLM context output, allowing the <c>--help</c> text to remain concise while
/// the AI description includes richer usage guidance and scenario information.
/// </summary>
/// <param name="description">
/// The AI-focused description. Should state what the command does, what it returns,
/// and in which scenarios an AI agent should use it.
/// </param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class LlmDescriptionAttribute(string description) : Attribute
{
    /// <summary>
    /// Gets the AI-focused description with usage guidance and scenario information.
    /// </summary>
    public string Description { get; } = description;
}
