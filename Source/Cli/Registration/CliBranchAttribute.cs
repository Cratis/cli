// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Registration;

/// <summary>
/// Marks a static class as a CLI branch node. Nesting of attributed types defines
/// the parent–child relationship; the source generator walks <c>ContainingType</c>
/// to build the full route.
/// </summary>
/// <param name="name">The CLI name for this branch segment (e.g. "observers").</param>
/// <param name="description">The help description shown for this branch.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CliBranchAttribute(string name, string description) : Attribute
{
    /// <summary>
    /// Gets the CLI name for this branch segment (e.g. "observers").
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the help description shown for this branch.
    /// </summary>
    public string Description { get; } = description;
}
