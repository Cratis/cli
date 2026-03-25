// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Registration;

/// <summary>
/// Marks a command class for automatic CLI registration. The source generator discovers
/// all types with this attribute and produces the Spectre.Console.Cli registration code.
/// </summary>
/// <param name="name">The leaf command name (e.g. "list", "show", "tail").</param>
/// <param name="description">The help description for this command.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CliCommandAttribute(string name, string description) : Attribute
{
    /// <summary>
    /// Gets the leaf command name (e.g. "list", "show", "tail").
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the help description for this command.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// Gets or sets the branch this command belongs to.
    /// Use <c>typeof(ChronicleBranch.Observers)</c> for type-safe references.
    /// When <see langword="null"/>, the command is registered at the root level.
    /// </summary>
    public Type? Branch { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this command is hidden from help output.
    /// </summary>
    public bool IsHidden { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this command should be excluded from
    /// the LLM context descriptor.
    /// </summary>
    public bool ExcludeFromLlm { get; init; }
}
