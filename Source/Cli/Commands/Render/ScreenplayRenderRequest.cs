// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Represents one trusted Screenplay artifact-planning request.
/// </summary>
/// <param name="SourcePath">The resolved file or folder containing one logical application.</param>
/// <param name="ApplicationName">The explicit application identity.</param>
/// <param name="Target">The statically bundled renderer target.</param>
/// <param name="ProjectName">The generated project name, defaulting to the application name.</param>
/// <param name="RootNamespace">The requested root namespace, defaulting to the application name.</param>
internal sealed record ScreenplayRenderRequest(
    string SourcePath,
    string ApplicationName,
    string Target,
    string? ProjectName = null,
    string? RootNamespace = null);
