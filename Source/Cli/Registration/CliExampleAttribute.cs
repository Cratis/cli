// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Registration;

/// <summary>
/// Defines a usage example for a CLI command, shown in help text.
/// </summary>
/// <param name="args">The example arguments as they would appear on the command line.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CliExampleAttribute(params string[] args) : Attribute
{
    /// <summary>
    /// Gets the example arguments as they would appear on the command line.
    /// </summary>
    public string[] Args { get; } = args;

    /// <summary>
    /// Gets or sets the command name this example belongs to.
    /// Only needed when a class has multiple <see cref="CliCommandAttribute"/> registrations
    /// (e.g. <c>PrintCompletionCommand</c> registered as bash, zsh, and fish).
    /// </summary>
    public string? CommandName { get; init; }
}
