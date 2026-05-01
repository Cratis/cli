// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Registration;

/// <summary>
/// Provides an AI-specific option description for the LLM context descriptor.
/// When present, overrides the auto-detected description from the Settings class
/// <c>[CommandOption]</c> / <c>[CommandArgument]</c> attributes.
/// </summary>
/// <param name="name">The option name as shown to the AI.</param>
/// <param name="type">The option type (e.g. "string", "guid").</param>
/// <param name="description">The AI-specific description with usage hints.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class LlmOptionAttribute(string name, string type, string description) : Attribute
{
    /// <summary>
    /// Gets the option name as shown to the AI (e.g. "&lt;OBSERVER_ID&gt;" or "-t, --type").
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the option type (e.g. "string", "guid", "int", "bool").
    /// </summary>
    public string Type { get; } = type;

    /// <summary>
    /// Gets the AI-specific description with usage hints and context.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// Gets or sets the command name this option belongs to.
    /// Only needed when a class has multiple <see cref="CliCommandAttribute"/> registrations.
    /// </summary>
    public string? CommandName { get; init; }
}
