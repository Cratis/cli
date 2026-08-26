// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the physical and logical source metadata for one loaded project compilation.
/// </summary>
/// <param name="ProjectPath">The actual project file path used internally when invoking source adapters.</param>
/// <param name="LogicalProjectPath">The relocation-safe workspace-relative project path.</param>
/// <param name="SourceContext">The stable source identity and display-path context.</param>
public record ScreenplayProjectSource(
    string ProjectPath,
    string LogicalProjectPath,
    DotNetProjectSourceContext SourceContext)
{
    /// <summary>
    /// Gets the semantic role the workspace host assigned to the project.
    /// </summary>
    public DotNetProjectRole Role { get; init; } = DotNetProjectRole.Application;

    /// <summary>
    /// Gets the physical source root used only while constructing source-adapter inputs.
    /// </summary>
    public string? SourceRoot { get; init; }
}
