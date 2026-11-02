// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Identifies the source and project selection for a compilation load.
/// </summary>
/// <param name="TargetPath">The solution or project file to load.</param>
/// <param name="IncludeAllProjects">Whether to retain every non-spec C# project.</param>
/// <param name="TargetFramework">The requested target framework, if any.</param>
internal sealed record ScreenplayCompilationRequest(string TargetPath, bool IncludeAllProjects, string? TargetFramework);
