// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Represents one trusted Screenplay artifact-planning request.
/// </summary>
/// <param name="SourcePath">The resolved file or folder containing one logical application.</param>
/// <param name="ApplicationName">The explicit application identity and generated root namespace.</param>
/// <param name="Target">The statically bundled renderer target.</param>
internal sealed record ScreenplayRenderRequest(string SourcePath, string ApplicationName, string Target);
