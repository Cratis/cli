// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Enables authored source locations only for a known destination that can read every model document.
/// </summary>
internal static class RenderSourceLineMapping
{
    internal static ArtifactRenderProfile Apply(ArtifactRenderProfile profile, SemanticCompilation compilation, string? modelRoot, string? destination)
    {
        if (modelRoot is null || destination is null || !compilation.Documents.Documents.All(document => File.Exists(Path.Combine(modelRoot, document.DisplayPath))))
        {
            return profile;
        }

        var relativeRoot = Path.GetRelativePath(Path.GetFullPath(destination), Path.GetFullPath(modelRoot)).Replace('\\', '/');
        if (Path.IsPathRooted(relativeRoot))
        {
            return profile;
        }

        try
        {
            return CratisRendering.WithSourceLineMapping(profile, relativeRoot == "." ? string.Empty : relativeRoot, compilation);
        }
        catch (InvalidArtifactRenderContract)
        {
            // Paths that cannot be represented safely in a #line directive keep Stage's default mapping.
            return profile;
        }
    }
}
